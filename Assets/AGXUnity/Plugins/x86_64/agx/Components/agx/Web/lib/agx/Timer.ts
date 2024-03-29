
module agx
{
  /**
  Timer class for measuring elapsed time.
  */
  export class Timer
  {
    /**
    Constructor
    */
    constructor (startImmediately : boolean = false)
    {
      this.reset(startImmediately);
    }

    /**
    Start the timer.
    */
    start()
    {
      if (this.running)
        return;

      this.startTime = performance.now();
      this.running = true;
    }

    /**
    Stop the timer.
    \return The total measured time
    */
    stop() : number
    {
      if (!this.running)
        return;

      var currentTime = performance.now();
      this.totalTime += currentTime - this.startTime;
      this.running = false;
      return this.totalTime;
    }

    /**
    Reset the timer
    \param startAfterReset Restart the timer if true
    \return The total measured time
    */
    reset(startAfterReset : boolean = false) : number
    {
      var totalTime = this.getCurrentTime();
      this.running = false;
      this.startTime = 0;
      this.totalTime = 0;

      if (startAfterReset)
        this.start();

      return totalTime;
    }

    /**
    \return The measured time in milliseconds.
    */
    getTime() : number
    {
      return this.totalTime;
    }

    /**
    \return The elapsed time in milliseconds.
    */
    getCurrentTime() : number
    {
      return this.running ? (performance.now() - this.startTime) + this.totalTime : this.getTime();
    }

    //////////////////////////////////////////////////
    private running : boolean;
    private startTime : number;
    private totalTime : number;
  };
}