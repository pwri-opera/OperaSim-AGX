declare variable $f external;
declare variable $d := doc( $f );


"Update.UpdateSpace.UpdateBroadPhase",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="UpdateBroadPhase"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.ComputeNarrowPhaseContacts",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="ComputeNarrowPhaseContacts"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="ComputeNarrowPhaseContacts"]/AccumulatedComputeCost ) ,

"Update.UpdateSpace.SynchronizeBounds",
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="SynchronizeBounds"]/AccumulatedWallTime ) ,
data( $d//Task[@name="Update"]/Task[@name="UpdateSpace"]/Task[@name="SynchronizeBounds"]/AccumulatedComputeCost ) ,

""