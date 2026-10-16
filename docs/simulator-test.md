# Simulator-Based ROS Pipeline Test

How to run the real Unity simulator against the real ROS 2 middleware and measure
topic latency / drive machines — the setup used to reproduce issue #103 (EKF lag)
and to verify the `extendSendQueue` toggle.

## Components

| Piece | What it is |
|---|---|
| Unity PlayMode test | `Assets/Tests/PlayMode/RosPipelineLagTest.cs` — loads `GameScene`, sets `ActionMode=3`, calls `ROSConnection.Connect("127.0.0.1", 10000)` via reflection, then publishes a wall-clock probe topic `/lag_probe` (`std_msgs/Float64` carrying epoch seconds) at 20 Hz for `RunSeconds` (currently 420 s). |
| ROS middleware | `docker-hackathon-sample` image `opera-hackathon-sample` — bundles `ros_tcp_endpoint`, `com3_msgs`, `task_manager`, `ic120_autonomy`, nav2, `ic120_unity`. |
| Lag monitor | `/tmp/queue_repro/lag_monitor.py` (see "Helper scripts" below) — subscribes to the stamped topics and `/lag_probe`, writes stats to `/tmp/lag_stats.log` every 5 s. |

## Prerequisites

- Unity Editor `2022.3.62f3` at `~/Unity/Hub/Editor/2022.3.62f3/Editor/Unity`.
- Project `Library/` already imported once (cold import takes a long time — copy an existing `Library/` if needed).
- Docker + image `opera-hackathon-sample:latest` (build with `docker-hackathon-sample/build.sh` if absent).
- `com3_msgs` is NOT on the host — topics that use it (`track_cmd`, `fluid_pressure`, `rot_dump_cmd`) only work when the endpoint runs inside the middleware container. A standalone host endpoint works but floods `Unknown message class` errors and distorts timing measurements.

## Run order

### 1. Middleware container (endpoint + task manager)

```bash
docker rm -f opera-test 2>/dev/null
docker run -d --name opera-test --network host opera-hackathon-sample sleep infinity

# terminal 1: endpoint (listens on :10000)
docker exec -d opera-test bash -c \
  'source /ws/install/setup.bash && ros2 launch ros_tcp_endpoint endpoint.py > /tmp/endpoint.log 2>&1'

# terminal 2: hackathon task stack (autonomy, nav2, task manager)
docker exec -d opera-test bash -c \
  'source /ws/install/setup.bash && ros2 launch task_manager hackathon_sample.launch.py > /tmp/hackathon.log 2>&1'
```

Verify: `ss -tln | grep 10000` shows a listener; `docker exec opera-test tail /tmp/hackathon.log` shows `--- TASK MANAGER READY ---`.

### 2. Lag monitor (optional, host or container)

```bash
LD_LIBRARY_PATH=/opt/ros/jazzy/lib PYTHONPATH=/opt/ros/jazzy/lib/python3.12/site-packages \
  python3 /tmp/queue_repro/lag_monitor.py &
# stats land in /tmp/lag_stats.log
```

Inside the container use plain `python3` (ROS already in env); subscribe inside the
container if the host ROS distro differs (Jazzy↔Humble DDS bridged fine here, but
in-container is safer).

### 3. Unity sim (PlayMode test)

```bash
cd <repo>
~/Unity/Hub/Editor/2022.3.62f3/Editor/Unity \
  -projectPath <repo> \
  -runTests -testPlatform PlayMode \
  -testFilter "RosPipelineLagTest" \
  -testResults /tmp/unity_test_results.xml \
  -logFile /tmp/unity_test.log
```

The test replaces the manual UI flow: it skips the widget and does what the
start button does — `GlobalVariables.changeActionMode(3)` (physics resumes —
look for `Physics Start!` in the log) — plus `ROSConnection.Connect`. The widget
only adds a countdown UI and camera setup; it does not gate machine input.

Scene-loaded sim takes ~30 s; the run lasts `RunSeconds` (420 s). Machines spawn
as `ic120_0`, `zx200` and start publishing once connected.

## What to look for

| Signal | Where | Meaning |
|---|---|---|
| `RegisterSubscriber/RegisterPublisher ... OK` | `docker exec opera-test tail /tmp/endpoint.log` | topics registered — expect `/ic120_0/cmd_vel`, `/ic120_0/track_cmd`, `/ic120_0/rot_dump_cmd`, `/tf`, etc. |
| `PROBE transport-lag` | `/tmp/lag_stats.log` | true enqueue→delivery delay in seconds (unambiguous; includes Unity queue + TCP + endpoint). |
| `skewlag` per topic | `/tmp/lag_stats.log` | `arrival_wall − msg.stamp` relative to min — upper bound, contaminated by sim-vs-wall slowdowns during scene load. |
| `Queue full!` warnings | `/tmp/unity_test.log` | Unity-side send congestion → drives the bug from #103. |
| `x= y= z=` drift | `docker exec opera-test tail /tmp/pos.log` (odom watcher) | machine actually moving. |
| `TASK RECEIVED` / `Step N` | `/tmp/hackathon.log` | task sequence progress — zx200 digs first, ic120 is dispatched later. |

## A/B: enlarged send queues

All affected publishers have a serialized toggle (default **off** = pre-`1b7d715`
queue depth 10):

- `extendSendQueue` on `OdometryPublisher`, `JointStatePublisher`, `SoilVolumePublisher`,
  `FluidPressureArrayPublisher`, `ImuPublisher` (base classes → all machine variants)
- `m_ExtendSendQueue` on `ROSClockPublisher`

Toggle in the Inspector on the machine prefab/component, or flip the C# default.
`true` → `RegisterPublisher(topic, ~1 s of msgs)` (post-`1b7d715`); `false` →
default depth 10.

## Helper scripts (recreate if /tmp was cleaned)

<details><summary>lag_monitor.py</summary>

```python
import time, threading
import rclpy
from rclpy.node import Node
from rclpy.qos import QoSProfile
from nav_msgs.msg import Odometry
from sensor_msgs.msg import JointState, Imu, PointCloud2
from rosgraph_msgs.msg import Clock
from std_msgs.msg import Float64

STAMPED = {
  "/ic120_0/odom_pose": (Odometry, lambda m: m.header.stamp),
  "/ic120_0/global_pose": (Odometry, lambda m: m.header.stamp),
  "/ic120_0/joint_states": (JointState, lambda m: m.header.stamp),
  "/ic120_0/upper_body_rot": (Imu, lambda m: m.header.stamp),
  "/zx200/odom_pose": (Odometry, lambda m: m.header.stamp),
  "/zx200/joint_states": (JointState, lambda m: m.header.stamp),
  "clock": (Clock, lambda m: m.clock),
  "/area_pc": (PointCloud2, lambda m: m.header.stamp),
}
WINDOW = 30.0
skew_hist, probe_lags, count = {}, [], {}

def ss(s): return s.sec + s.nanosec * 1e-9

class Mon(Node):
    def __init__(self):
        super().__init__("lag_monitor"); qos = QoSProfile(depth=10)
        for t,(cls,g) in STAMPED.items():
            skew_hist[t]=[]; count[t]=0
            self.create_subscription(cls,t,self._mk(t,g),qos)
        count["/lag_probe"]=0
        self.create_subscription(Float64,"/lag_probe",self._p,qos)
    def _mk(self,t,g):
        def cb(m):
            n=time.time(); skew_hist[t].append((n,n-ss(g(m)))); count[t]+=1
        return cb
    def _p(self,m):
        probe_lags.append((time.time(),time.time()-m.data)); count["/lag_probe"]+=1

def pct(a,p):
    if not a: return 0.0
    return sorted(a)[int(p*(len(a)-1))]
def summ(v):
    if not v: return "n=0"
    return f"n={len(v)} mean={sum(v)/len(v):.3f} p95={pct(v,0.95):.3f} max={max(v):.3f}"

def rep():
    t0=time.time()
    while True:
        time.sleep(5); now=time.time(); out=[]
        pl=[x[1] for x in probe_lags if now-x[0]<5.5]
        del probe_lags[:-100000]
        out.append("PROBE transport-lag: "+summ(pl))
        for t in STAMPED:
            h=[x for x in skew_hist[t] if now-x[0]<WINDOW]; skew_hist[t]=h
            r=[x[1] for x in h if now-x[0]<5.5]
            if not r: out.append(f"{t}: NO DATA (total={count[t]})"); continue
            off=min(x[1] for x in h); out.append(f"{t}: skewlag {summ([s-off for s in r])}")
        open("/tmp/lag_stats.log","a").write(f"--- t={now-t0:.0f}s ---\n"+"\n".join(out)+"\n")

threading.Thread(target=rep,daemon=True).start()
rclpy.init(); rclpy.spin(Mon())
```
</details>

<details><summary>odom watcher (run inside the container)</summary>

```bash
docker exec -d opera-test bash -c 'source /ws/install/setup.bash && python3 -c "
import rclpy,time,threading
from rclpy.node import Node
from nav_msgs.msg import Odometry
p=[]
class W(Node):
    def __init__(self):
        super().__init__(\"w\")
        self.create_subscription(Odometry,\"/ic120_0/odom_pose\",self.cb,10)
    def cb(self,m):
        q=m.pose.pose.position; p.append((time.time(),q.x,q.y,q.z))
def rep():
    while True:
        time.sleep(5)
        if p:
            t,x,y,z=p[-1]; open(\"/tmp/pos.log\",\"a\").write(f\"x={x:9.3f} y={y:9.3f} z={z:9.3f} n={len(p)}\\n\")
rclpy.init();n=W();threading.Thread(target=rep,daemon=True).start();rclpy.spin(n)
"'
```

</details>

<details><summary>drive_ic120.py — publish /ic120_0/cmd_vel (manual movement check)</summary>

```python
import time, threading, sys, rclpy
from rclpy.node import Node
from rclpy.qos import QoSProfile
from geometry_msgs.msg import Twist
from nav_msgs.msg import Odometry
SECS=float(sys.argv[1]) if len(sys.argv)>1 else 60
SPD=float(sys.argv[2]) if len(sys.argv)>2 else 0.5
pos=[]
class D(Node):
    def __init__(self):
        super().__init__("d"); self.t0=time.time()
        self.pub=self.create_publisher(Twist,"/ic120_0/cmd_vel",10)
        self.create_subscription(Odometry,"/ic120_0/odom_pose",
            lambda m: pos.append((m.pose.pose.position.x,m.pose.pose.position.y)),QoSProfile(depth=10))
def pub(n):
    m=Twist(); m.linear.x=SPD
    while time.time()-n.t0<SECS: n.pub.publish(m); time.sleep(0.1)
rclpy.init(); n=D(); threading.Thread(target=pub,args=(n,),daemon=True).start(); rclpy.spin(n)
```

Requires the container endpoint (host Jazzy lacks `com3_msgs` — Twist works, but
the fluid-pressure error storm skews measurements).

</details>

## Known caveats

- **Measured lag magnitudes in this setup are inflated.** Under the real
  middleware stack the probe showed ~16 s (queue=10) → ~24 s (queue=60); the
  user's ~5 s report is the same mechanism at a lighter load.
- `ros2 topic hz/echo` on the **host** breaks when the container is up
  (Humble↔Jazzy daemon XMLRPC clash) — run ROS CLI inside `opera-test` instead.
- Sudo-free `tc netem` is unavailable here, so no artificial link shaping; real
  send-side congestion already exists (`Queue full!` warnings fire).
- `Physics Start!` must appear in the Unity log — if absent, `ActionMode` never
  reached 3 and the machines won't move regardless of ROS traffic.
- The task manager dispatches **zx200 first**; the ic120 may legitimately sit at
  `0,0,0` until the dig phase completes — give the run time before concluding it
  can't move.
