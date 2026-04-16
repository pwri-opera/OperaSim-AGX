#!/usr/bin/env python3
"""
Test script to verify GitHub issue #67 is fixed.

Issue #67: ZX200's swing axis moves in opposite direction near 180°
Expected: When swing is at +179° and receives command for -179°, it should move +2° (shortest path)
Actual (before fix): It moved -358° (long path)

This script sends front_cmd position commands and monitors joint_states during movement
to verify the swing axis takes the shortest path across the ±π boundary.
"""

import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState
from com3_msgs.msg import JointCmd
import math
import time
from collections import deque


class Issue67TestNode(Node):
    def __init__(self):
        super().__init__('issue_67_test_node')
        
        # Parameters
        self.declare_parameter('machine_name', 'zx200')
        self.machine_name = self.get_parameter('machine_name').value
        
        # Joint names for front_cmd (4 joints: swing, boom, arm, bucket)
        self.front_joint_names = ['swing_joint', 'boom_joint', 'arm_joint', 'bucket_joint']
        
        # Subscribe to joint_states
        self.joint_state_sub = self.create_subscription(
            JointState,
            f'/{self.machine_name}/joint_states',
            self.joint_state_callback,
            10
        )
        
        # Publish to front_cmd
        self.front_cmd_pub = self.create_publisher(
            JointCmd,
            f'/{self.machine_name}/front_cmd',
            10
        )
        
        # Store joint states
        self.current_joint_states = None
        self.joint_state_history = deque(maxlen=1000)
        
        # Test parameters
        self.test_duration = 5.0  # seconds to monitor movement
        self.settle_time = 1.0  # seconds to wait before starting test
        self.tolerance = 0.1  # tolerance for position comparison (radians)
        
        self.get_logger().info(f'Issue #67 Test Node initialized for machine: {self.machine_name}')
        self.get_logger().info(f'Subscribing to: /{self.machine_name}/joint_states')
        self.get_logger().info(f'Publishing to: /{self.machine_name}/front_cmd')

    def joint_state_callback(self, msg):
        """Callback to receive joint states."""
        self.current_joint_states = msg
        
        # Store relevant joint states (swing, boom, arm, bucket)
        joint_data = {
            'header': msg.header.stamp,
            'swing': None,
            'boom': None,
            'arm': None,
            'bucket': None
        }
        
        for i, name in enumerate(msg.name):
            if name in self.front_joint_names:
                joint_data[name.replace('_joint', '')] = {
                    'position': msg.position[i],
                    'velocity': msg.velocity[i],
                    'effort': msg.effort[i]
                }
        
        self.joint_state_history.append(joint_data)

    def wait_for_joint_states(self, timeout=10.0):
        """Wait until joint states are received."""
        start_time = time.time()
        while self.current_joint_states is None:
            rclpy.spin_once(self, timeout_sec=0.1)
            if time.time() - start_time > timeout:
                self.get_logger().error('Timeout waiting for joint states')
                return False
        return True

    def get_swing_position(self):
        """Get current swing joint position."""
        if self.current_joint_states is None:
            return None
        for i, name in enumerate(self.current_joint_states.name):
            if name == 'swing_joint':
                return self.current_joint_states.position[i]
        return None

    def wait_for_swing_position(self, target_pos, timeout=30.0, tolerance=0.05):
        """
        Wait until swing reaches target position within tolerance.
        
        Args:
            target_pos: Target position in radians
            timeout: Maximum time to wait in seconds
            tolerance: Position tolerance in radians
            
        Returns:
            True if target reached, False if timeout
        """
        start_time = time.time()
        while time.time() - start_time < timeout:
            rclpy.spin_once(self, timeout_sec=0.1)
            current_pos = self.get_swing_position()
            if current_pos is not None:
                diff = abs(self.shortest_angular_distance(current_pos, target_pos))
                if diff < tolerance:
                    self.get_logger().info(f'Swing reached target: {current_pos:.4f} rad (target: {target_pos:.4f} rad, diff: {math.degrees(diff):.2f}°)')
                    return True
                else:
                    # Log progress every second
                    elapsed = time.time() - start_time
                    if int(elapsed) > int(elapsed - 0.1):
                        self.get_logger().info(f'Waiting for swing: current {current_pos:.4f} rad, target {target_pos:.4f} rad, diff {math.degrees(diff):.2f}°')
        
        current_pos = self.get_swing_position()
        self.get_logger().error(f'Timeout waiting for swing to reach {target_pos:.4f} rad. Current: {current_pos:.4f} rad')
        return False

    def normalize_angle(self, angle):
        """Normalize angle to [-π, π]."""
        while angle > math.pi:
            angle -= 2 * math.pi
        while angle < -math.pi:
            angle += 2 * math.pi
        return angle

    def shortest_angular_distance(self, from_angle, to_angle):
        """Calculate the shortest angular distance between two angles."""
        diff = to_angle - from_angle
        return self.normalize_angle(diff)

    def send_position_command(self, positions):
        """Send position command to front_cmd topic."""
        cmd = JointCmd()
        cmd.joint_name = self.front_joint_names
        cmd.control_type = 0  # 0 = position control
        cmd.position = positions
        cmd.velocity = [0.0] * len(positions)
        cmd.effort = [0.0] * len(positions)
        
        self.front_cmd_pub.publish(cmd)
        self.get_logger().info(f'Sent position command: {positions}')

    def test_swing_boundary_crossing(self):
        """
        Test swing axis crossing the ±π boundary.
        
        This test moves the swing from near +π to near -π and verifies
        it takes the shortest path (small movement) instead of the long path.
        """
        self.get_logger().info('='*60)
        self.get_logger().info('Testing Issue #67: Swing axis boundary crossing')
        self.get_logger().info('='*60)
        
        # Wait for initial joint states
        if not self.wait_for_joint_states():
            return False
        
        initial_swing = self.get_swing_position()
        self.get_logger().info(f'Initial swing position: {initial_swing:.4f} rad ({math.degrees(initial_swing):.2f}°)')
        
        # Define test positions
        # Position 1: Near +π (e.g., +179° = +3.124 rad)
        pos1_swing = math.radians(179.0)
        # Position 2: Near -π (e.g., -179° = -3.124 rad)
        pos2_swing = math.radians(-179.0)
        
        # Keep other joints at raised positions to avoid ground interference
        boom_pos = - math.radians(45.0)  # Raise boom 45°
        arm_pos = math.radians(90.0)   # Extend arm 90°
        bucket_pos = math.radians(0.0) # Keep bucket level
        
        # Test 1: Move from near +π to near -π
        self.get_logger().info('\n--- Test 1: Moving from +179° to -179° ---')
        self.get_logger().info(f'Expected: Swing should move +2° (shortest path)')
        self.get_logger().info(f'Bug (if present): Swing would move -358° (long path)')
        
        # First, move to position 1
        positions1 = [pos1_swing, boom_pos, arm_pos, bucket_pos]
        self.send_position_command(positions1)
        
        # Wait for swing to reach position 1
        self.get_logger().info('Waiting for swing to reach +179°...')
        if not self.wait_for_swing_position(pos1_swing, timeout=30.0, tolerance=0.05):
            self.get_logger().error('Failed to reach initial position')
            return False
        
        start_swing = self.get_swing_position()
        self.get_logger().info(f'Start position: {start_swing:.4f} rad ({math.degrees(start_swing):.2f}°)')
        
        # Clear history before movement
        self.joint_state_history.clear()
        
        # Move to position 2
        positions2 = [pos2_swing, boom_pos, arm_pos, bucket_pos]
        self.send_position_command(positions2)
        
        # Monitor movement
        self.get_logger().info('Monitoring movement...')
        start_time = time.time()
        swing_positions = []
        
        while time.time() - start_time < self.test_duration:
            rclpy.spin_once(self, timeout_sec=0.05)
            current_swing = self.get_swing_position()
            if current_swing is not None:
                swing_positions.append(current_swing)
        
        end_swing = self.get_swing_position()
        self.get_logger().info(f'End position: {end_swing:.4f} rad ({math.degrees(end_swing):.2f}°)')
        
        # Analyze movement
        if len(swing_positions) > 1:
            total_movement = 0.0
            for i in range(1, len(swing_positions)):
                diff = self.shortest_angular_distance(swing_positions[i-1], swing_positions[i])
                total_movement += abs(diff)
            
            expected_movement = abs(self.shortest_angular_distance(start_swing, pos2_swing))
            self.get_logger().info(f'Total movement detected: {math.degrees(total_movement):.2f}°')
            self.get_logger().info(f'Expected movement (shortest path): {math.degrees(expected_movement):.2f}°')
            
            # Check if movement is reasonable (should be close to expected, not the long path)
            long_path_movement = abs(2 * math.pi - expected_movement)
            self.get_logger().info(f'Long path movement would be: {math.degrees(long_path_movement):.2f}°')
            
            if total_movement < (expected_movement + self.tolerance):
                self.get_logger().info('✓ TEST PASSED: Swing took the shortest path')
                return True
            elif total_movement > (long_path_movement - self.tolerance):
                self.get_logger().error('✗ TEST FAILED: Swing took the long path (Issue #67 not fixed)')
                return False
            else:
                self.get_logger().warning(f'? TEST INCONCLUSIVE: Movement ({math.degrees(total_movement):.2f}°) between expected short ({math.degrees(expected_movement):.2f}°) and long ({math.degrees(long_path_movement):.2f}°) paths')
                return None
        else:
            self.get_logger().error('Could not analyze movement - insufficient data')
            return False

    def test_reverse_direction(self):
        """
        Test the reverse direction: from near -π to near +π.
        """
        self.get_logger().info('\n--- Test 2: Moving from -179° to +179° ---')
        
        # Position 1: Near -π
        pos1_swing = math.radians(-179.0)
        # Position 2: Near +π
        pos2_swing = math.radians(179.0)
        
        # Keep other joints at raised positions to avoid ground interference
        boom_pos = - math.radians(45.0)  # Raise boom 45°
        arm_pos = math.radians(90.0)   # Extend arm 90°
        bucket_pos = math.radians(0.0) # Keep bucket level
        
        # Move to position 1
        positions1 = [pos1_swing, boom_pos, arm_pos, bucket_pos]
        self.send_position_command(positions1)
        
        # Wait for swing to reach position 1
        self.get_logger().info('Waiting for swing to reach -179°...')
        if not self.wait_for_swing_position(pos1_swing, timeout=30.0, tolerance=0.05):
            self.get_logger().error('Failed to reach initial position')
            return False
        
        start_swing = self.get_swing_position()
        self.get_logger().info(f'Start position: {start_swing:.4f} rad ({math.degrees(start_swing):.2f}°)')
        
        self.joint_state_history.clear()
        
        # Move to position 2
        positions2 = [pos2_swing, boom_pos, arm_pos, bucket_pos]
        self.send_position_command(positions2)
        
        # Monitor movement
        start_time = time.time()
        swing_positions = []
        
        while time.time() - start_time < self.test_duration:
            rclpy.spin_once(self, timeout_sec=0.05)
            current_swing = self.get_swing_position()
            if current_swing is not None:
                swing_positions.append(current_swing)
        
        end_swing = self.get_swing_position()
        self.get_logger().info(f'End position: {end_swing:.4f} rad ({math.degrees(end_swing):.2f}°)')
        
        # Analyze movement
        if len(swing_positions) > 1:
            total_movement = 0.0
            for i in range(1, len(swing_positions)):
                diff = self.shortest_angular_distance(swing_positions[i-1], swing_positions[i])
                total_movement += abs(diff)
            
            expected_movement = abs(self.shortest_angular_distance(start_swing, pos2_swing))
            self.get_logger().info(f'Total movement detected: {math.degrees(total_movement):.2f}°')
            self.get_logger().info(f'Expected movement (shortest path): {math.degrees(expected_movement):.2f}°')
            
            long_path_movement = abs(2 * math.pi - expected_movement)
            self.get_logger().info(f'Long path movement would be: {math.degrees(long_path_movement):.2f}°')
            
            if total_movement < (expected_movement + self.tolerance):
                self.get_logger().info('✓ TEST PASSED: Swing took the shortest path')
                return True
            elif total_movement > (long_path_movement - self.tolerance):
                self.get_logger().error('✗ TEST FAILED: Swing took the long path (Issue #67 not fixed)')
                return False
            else:
                self.get_logger().warning(f'? TEST INCONCLUSIVE')
                return None
        else:
            self.get_logger().error('Could not analyze movement - insufficient data')
            return False

    def run_tests(self):
        """Run all tests."""
        self.get_logger().info('Starting Issue #67 verification tests...')
        
        # Wait for system to be ready
        time.sleep(2.0)
        
        # Run test 1
        result1 = self.test_swing_boundary_crossing()
        
        # Run test 2 (reverse direction)
        result2 = self.test_reverse_direction()
        
        # Summary
        self.get_logger().info('\n' + '='*60)
        self.get_logger().info('TEST SUMMARY')
        self.get_logger().info('='*60)
        self.get_logger().info(f'Test 1 (+179° to -179°): {"PASSED" if result1 else "FAILED" if result1 is False else "INCONCLUSIVE"}')
        self.get_logger().info(f'Test 2 (-179° to +179°): {"PASSED" if result2 else "FAILED" if result2 is False else "INCONCLUSIVE"}')
        
        if result1 and result2:
            self.get_logger().info('\n✓ Issue #67 appears to be FIXED')
            return True
        elif result1 is False or result2 is False:
            self.get_logger().error('\n✗ Issue #67 is NOT fixed')
            return False
        else:
            self.get_logger().warning('\n? Tests were inconclusive')
            return None


def main():
    rclpy.init()
    
    test_node = Issue67TestNode()
    
    try:
        result = test_node.run_tests()
    except Exception as e:
        test_node.get_logger().error(f'Test failed with exception: {e}')
        import traceback
        traceback.print_exc()
        result = False
    finally:
        test_node.destroy_node()
        rclpy.shutdown()
    
    return 0 if result else 1


if __name__ == '__main__':
    import sys
    sys.exit(main())
