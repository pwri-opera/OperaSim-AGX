var agx;
(function (agx) {
    /**
    Timer class for measuring elapsed time.
    */
    var Timer = (function () {
        /**
        Constructor
        */
        function Timer(startImmediately) {
            if (startImmediately === void 0) { startImmediately = false; }
            this.reset(startImmediately);
        }
        /**
        Start the timer.
        */
        Timer.prototype.start = function () {
            if (this.running)
                return;
            this.startTime = performance.now();
            this.running = true;
        };
        /**
        Stop the timer.
        \return The total measured time
        */
        Timer.prototype.stop = function () {
            if (!this.running)
                return;
            var currentTime = performance.now();
            this.totalTime += currentTime - this.startTime;
            this.running = false;
            return this.totalTime;
        };
        /**
        Reset the timer
        \param startAfterReset Restart the timer if true
        \return The total measured time
        */
        Timer.prototype.reset = function (startAfterReset) {
            if (startAfterReset === void 0) { startAfterReset = false; }
            var totalTime = this.getCurrentTime();
            this.running = false;
            this.startTime = 0;
            this.totalTime = 0;
            if (startAfterReset)
                this.start();
            return totalTime;
        };
        /**
        \return The measured time in milliseconds.
        */
        Timer.prototype.getTime = function () {
            return this.totalTime;
        };
        /**
        \return The elapsed time in milliseconds.
        */
        Timer.prototype.getCurrentTime = function () {
            return this.running ? (performance.now() - this.startTime) + this.totalTime : this.getTime();
        };
        return Timer;
    })();
    agx.Timer = Timer;
    ;
})(agx || (agx = {}));
