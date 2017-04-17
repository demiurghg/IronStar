using Fusion.Core.Mathematics;
using SharpDX;
using SharpDX.XAPO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Audio.XAPOExperiments
{
    class HRTFApo
    {
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TestXapoParams
    {
        public Vector3 postition;
    }


    /// <summary>
    /// A simple Ring Modulator Effect
    /// </summary>
    public class TestXapo : AudioProcessorBase<TestXapoParams>
    {
        private Stopwatch timer;

        public TestXapo()
        {
            RegistrationProperties = new RegistrationProperties()
            {
                Clsid = Utilities.GetGuidFromType(typeof(ModulatorEffect)),
                CopyrightInfo = "Copyright",
                FriendlyName = "Modulator",
                MaxInputBufferCount = 1,
                MaxOutputBufferCount = 1,
                MinInputBufferCount = 1,
                MinOutputBufferCount = 1,
                Flags = PropertyFlags.Default
            };
            timer = new Stopwatch();
            timer.Start();
        }

        private int _counter;
        public override void Process(BufferParameters[] inputProcessParameters, BufferParameters[] outputProcessParameters, bool isEnabled)
        {
            int frameCount = inputProcessParameters[0].ValidFrameCount;
            DataStream input = new DataStream(inputProcessParameters[0].Buffer, frameCount * InputFormatLocked.BlockAlign, true, true);
            DataStream output = new DataStream(inputProcessParameters[0].Buffer, frameCount * InputFormatLocked.BlockAlign, true, true);



            //Console.WriteLine("Process is called every: " + timer.ElapsedMilliseconds);
            timer.Reset(); timer.Start();

            // Use a linear ramp on intensity in order to avoir too much glitches
            float nextIntensity = Intensity;
            for (int i = 0; i < frameCount; i++, _counter++)
            {
                float left = input.Read<float>();
                float right = input.Read<float>();
                float intensity = (nextIntensity - lastIntensity) * (float)i / frameCount + lastIntensity;
                double vibrato = Math.Cos(2 * Math.PI * intensity * 400 * _counter / InputFormatLocked.SampleRate);
                output.Write((float)vibrato * left);
                output.Write((float)vibrato * right);
            }
            lastIntensity = nextIntensity;
        }

        private float lastIntensity = 0;

        public float Intensity { get; set; }
    }
}

