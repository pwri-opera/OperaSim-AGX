This document describes the format for TerrainMaterial specification files.
The basic structure is in .json format that the reflects the underlying class layout
of the TerrainMaterial class.

All parameters are in SI units and below follows a sample configuration file where
all the parameters have their units explained.

Note: The unit comments below exists for educational purposes, the JSON format
      does not support comments. They either have to be removed if this sample should
      be used. It is instead recommended to use one of the sample preset files as a base for usage.

This is a sample file for the "dirt_1" preset, augmented with unit comments.

{
   "BulkProperties" : {
      "adhesionOverlapFactor" : 0.050,       // Dimensionless
      "cohesion" : 12000.0,                  // Pa
      "density" : 1300.0,                    // kg/m
      "dilatancyAngle" : 0.2268928027592629, // Radians
      "frictionAngle" : 0.7504915783575616,  // Radians
      "maximumDensity" : 2000.0,             // kg/m
      "poissonsRatio" : 0.150,               // Dimensionless
      "swellFactor" : 1.280,                 // Dimensionless
      "youngsModulus" : 5000000.0            // Pa
   },
   "CompactionProperties" : {
      "angleOfReposeCompactionRate" : 24.0,        // Dimensionless
      "bankStatePhi" : 0.6666666666666666,         // Dimensionless
      "compactionTimeRelaxationConstant" : 0.050,  // Dimensionless
      "compressionIndex" : 0.110,                  // Dimensionless
      "hardeningConstantKE" : 1.0,                 // Dimensionless
      "hardeningConstantNE" : 0.08333333333333333, // Dimensionless
      "preconsolidationStress" : 98000.0,          // Pa
      "stressCutOffFraction" : 0.010               // Dimensionless
      "stressCutOffFraction" : 0.010               // Dimensionless
   },
   "ExcavationContactProperties" : {
      "aggregateStiffnessMultiplier" : 0.050,       // Dimensionless
      "depthDecayFactor" : 2.0,                     // Dimensionless
      "depthIncreaseFactor" : 1.0,                  // Dimensionless
      "excavationStiffnessMultiplier" : 1.0,        // Dimensionless
      "dilatancyAngleScalingFactor" : 0.261799,     // Radians
      "nominalDilatancyCompaction" : 1.0            // Dimensionless
   },
   "ParticleProperties" : {
      "particleCohesion" : 200.0,                   // Pa
      "particleFriction" : 0.40,                    // Dimensionless
      "particleRestitution" : 0.0,                  // Dimensionless
      "particleRollingResistance" : 0.10,           // Dimensionless
      "particleTerrainCohesion" : 200.0,            // Pa
      "particleTerrainFriction" : 0.70,             // Dimensionless
      "particleTerrainRestitution" : 0.0,           // Dimensionless
      "particleTerrainRollingResistance" : 0.70,    // Dimensionless
      "particleTerrainYoungsModulus" : 100000000.0, // Pa
      "particleYoungsModulus" : 10000000.0          // Pa
   },
   "description" : "DIRT_1" // String
}

