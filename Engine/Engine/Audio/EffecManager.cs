using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XAudio2;
using SharpDX.XAPO;
using SharpDX;

namespace Fusion.Engine.Audio
{
    public class AudioProcessorManager
    {
        private SubmixVoice _submixVoice;
        private VoiceSendDescriptor _sendDescriptor;
        private int _effectsCount;
        private const int SampleRate = 44100;
        private const int ChannelsCount = 2;

        public SubmixVoice Voice { get; private set; }

        public AudioProcessorManager(XAudio2 device, bool useFilter = false)
        {
            var voiceFlag = useFilter ? SubmixVoiceFlags.UseFilter : SubmixVoiceFlags.None; 
            _submixVoice = new SubmixVoice(device, ChannelsCount, SampleRate, voiceFlag, 1);

            var sendFlag = useFilter ? VoiceSendFlags.UseFilter : VoiceSendFlags.None;
            _sendDescriptor = new VoiceSendDescriptor(sendFlag, _submixVoice);
            _effectsCount = 0;
        }


        public void ConnectToSourceVoice(SourceVoice source)
        {
            if (source == null) return;

            source.SetOutputVoices(_sendDescriptor);
        }

        public void SetEffcts(params AudioProcessor[] apo)
        {
            var desc = apo.Select(a => new EffectDescriptor(a)).ToArray();
            _effectsCount = apo.Length;
            _submixVoice.SetEffectChain(desc);
        }

        public void SetEffectParameters<T>(int index, T parameters) where T : struct
        {
            if (index < _effectsCount)
                _submixVoice.SetEffectParameters(index, parameters);
        }

        public void FlushChain()
        {
            _submixVoice.SetEffectChain();
        }

        public void DisableEffects()
        {
            for (int i = 0; i < _effectsCount; i++)
            {
                _submixVoice.DisableEffect(i);
            }
        }

        public void EnableEffects()
        {
            for (int i = 0; i < _effectsCount; i++)
            {
                _submixVoice.EnableEffect(i);
            }
        }
    }
}
