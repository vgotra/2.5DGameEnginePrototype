namespace Engine.App;

    public struct FrameMetrics
    {
        private const int PrintInterval = 120;
        private double _totalFrameMs;
        private double _maxFrameMs;
        private long _totalFixedSteps;
        private long _totalSprites;
        private long _totalAllocatedBytes;
        private long _totalFixedBytes;
        private long _totalRenderBytes;
        private long _gen0;
        private long _gen1;
        private long _gen2;
        private int _frames;

        public void Add(double frameMs, int fixedSteps, int sprites, long allocatedBytes, long fixedBytes, long renderBytes, long gen0, long gen1, long gen2)
        {
            _totalFrameMs += frameMs;
            if (frameMs > _maxFrameMs) _maxFrameMs = frameMs;
            _totalFixedSteps += fixedSteps;
            _totalSprites += sprites;
            _totalAllocatedBytes += allocatedBytes;
            _totalFixedBytes += fixedBytes;
            _totalRenderBytes += renderBytes;
            _gen0 += gen0;
            _gen1 += gen1;
            _gen2 += gen2;
            _frames++;
            if (_frames >= PrintInterval) PrintAndReset();
        }

        private void PrintAndReset()
        {
            double avgFrameMs = _totalFrameMs / _frames;
            double avgAlloc = (double)_totalAllocatedBytes / _frames;
            double avgFixed = (double)_totalFixedBytes / _frames;
            double avgRender = (double)_totalRenderBytes / _frames;
            Console.WriteLine(
                $"metrics  frames={_frames,4}  avg={avgFrameMs,7:F2}ms  max={_maxFrameMs,7:F2}ms  " +
                $"sim={_totalFixedSteps,4}  sprites={_totalSprites,6}  alloc={avgAlloc,6:F1} B/frame  " +
                $"fixed={avgFixed,6:F1}  render={avgRender,6:F1}  gen0={_gen0} gen1={_gen1} gen2={_gen2}");
            _totalFrameMs = 0;
            _maxFrameMs = 0;
            _totalFixedSteps = 0;
            _totalSprites = 0;
            _totalAllocatedBytes = 0;
            _totalFixedBytes = 0;
            _totalRenderBytes = 0;
            _gen0 = 0;
            _gen1 = 0;
            _gen2 = 0;
            _frames = 0;
        }
    }

