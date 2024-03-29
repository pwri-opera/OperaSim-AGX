module agx
{
  // export var InvalidIndex = -1;

  export function AlignCeil( value : number, alignment : number ) : number
  {
    if ( value > 0 )
      return (value + (alignment-1) - ((value-1) % alignment));
    else
      return 0;
  }

  export function AlignFloor( value : number, alignment : number ) : number
  {
    return value - (value % alignment);
  }

  export function IsAligned( value : number, alignment : number ) : boolean
  {
    return value % alignment == 0;
  }

  export function Epsilon(): number
  {
    return 2.220460492503130808472633361816E-16;
  }
  
  export function toReadableNumber( value : number ) : string
  {
    var absValue : number = Math.abs(value);
    var exponentialLimit : number = 1e6;
    
    if (absValue == 0)
      // Special case for initialized values.
      return String(Math.round(value));
    else if (absValue < exponentialLimit && absValue > 1 / exponentialLimit)
      // Remove extra digits.
      return String(Math.round(value * exponentialLimit) / exponentialLimit);
    else
      // Make more readable.
      return String(value.toExponential(3));
  }
}
