﻿// This file is part of Aximo, a Game Engine written in C#. Web: https://github.com/AximoGames
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using GLib;
using OpenToolkit.Audio;
using OpenToolkit.Audio.OpenAL;

namespace Aximo.Engine.Audio
{

    public class AudioAmplifierModule : AudioModule
    {
        private Port[] InputChannels;
        private Port[] OutputChannels;

        public AudioAmplifierModule()
        {
            Name = "Amplifier";

            ConfigureInput("Left", 0);
            ConfigureInput("Right", 1);
            ConfigureOutput("Left", 0);
            ConfigureOutput("Right", 1);

            InputChannels = new Port[] { Inputs[0], Inputs[1] };
            OutputChannels = new Port[] { Outputs[0], Outputs[1] };
        }

        public float Volume = 0.5f;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void Process()
        {
            var inputChannels = InputChannels;
            var len = InputChannels.Length;
            var outputChannels = OutputChannels;

            var volume = Volume;

            for (var i = 0; i < len; i++)
                outputChannels[i].SetVoltage(inputChannels[i].GetVoltage() * volume);
        }
    }
}