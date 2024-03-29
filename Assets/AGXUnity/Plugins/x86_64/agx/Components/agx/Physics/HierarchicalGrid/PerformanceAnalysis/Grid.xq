declare variable $f external;
declare variable $d := doc( $f );

"Update.UpdateSpace.UpdateBroadPhase",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.TriggerCellReordering",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="TriggerCellReordering"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="TriggerCellReordering"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.ReorderCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ReorderCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ReorderCells"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.ClearCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ClearCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ClearCells"]/AccumulatedComputeCost ) ,

(:
"Update.UpdateSpace.UpdateBroadPhase.RemoveDeadCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="RemoveDeadCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="RemoveDeadCells"]/AccumulatedComputeCost ) ,
:)

"Update.UpdateSpace.UpdateBroadPhase.DisconnectDeadCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="DisconnectDeadCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="DisconnectDeadCells"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CalculateParticleCellAssignments",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CalculateParticleCellAssignments"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CalculateParticleCellAssignments"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CalculateGeometryCellAssignments",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CalculateGeometryCellAssignments"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CalculateGeometryCellAssignments"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CreateMissingCells:Particles",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CreateMissingCells:Particles"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CreateMissingCells:Particles"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CreateMissingCells:Geometries",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CreateMissingCells:Geometries"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CreateMissingCells:Geometries"]/AccumulatedComputeCost ) ,


"Update.UpdateSpace.UpdateBroadPhase.InitializeNewCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InitializeNewCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InitializeNewCells"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.SortCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="SortCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="SortCells"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CountCellOccupancy:Particles",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CountCellOccupancy:Particles"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CountCellOccupancy:Particles"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.CountCellOccupancy:Geometries",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CountCellOccupancy:Geometries"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="CountCellOccupancy:Geometries"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.AllocateCellContents",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="AllocateCellContents"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="AllocateCellContents"]/AccumulatedComputeCost ) ,


"Update.UpdateSpace.UpdateBroadPhase.InsertParticles",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InsertParticles"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InsertParticles"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.InsertGeometries",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InsertGeometries"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="InsertGeometries"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.ReorderSubsystems",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ReorderSubsystems"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="ReorderSubsystems"]/AccumulatedComputeCost ) ,


"Update.UpdateSpace.UpdateBroadPhase.UpdateOrientedGeometryBounds",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="UpdateOrientedGeometryBounds"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="UpdateOrientedGeometryBounds"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.FindOverlapPairs:IterateCells",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="FindOverlapPairs:IterateCells"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="FindOverlapPairs:IterateCells"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.FilterOverlaps",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="FilterOverlaps"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="FilterOverlaps"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateBroadPhase.GenerateGeometryBroadPhasePairs",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="GenerateGeometryBroadPhasePairs"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/Task[@name="GenerateGeometryBroadPhasePairs"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.UpdateContactState",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateContactState"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateContactState"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.ComputeNarrowPhaseContacts",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="ComputeNarrowPhaseContacts"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="ComputeNarrowPhaseContacts"]/AccumulatedComputeCost ) ,

""