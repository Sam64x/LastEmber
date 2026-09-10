using System;
using System.Collections.Generic;
using Godot;
namespace LastEmber;

public enum MeleeSoundPhase { Ignition, Swing, Hit, Miss, Critical, Charge, Ready }

// Authored layered stereo PCM; generated once per variant, not per attack or audio frame.
// Optional recorded layers in StrikeStyleData replace these without changing combat code.
public sealed class MeleeSoundBank : IDisposable
{
    private readonly Dictionary<(int,int,MeleeSoundPhase),AudioStreamWav> _cache=new();
    public AudioStream Get(int character,int variant,MeleeSoundPhase phase)
    {
        var key=(Math.Clamp(character,0,2),Math.Abs(variant)%3,phase);
        if(!_cache.TryGetValue(key,out var sound)){sound=Render(key.Item1,key.Item2,phase);_cache.Add(key,sound);}
        return sound;
    }
    public void Warm()
    {
        for(int character=0;character<3;character++)for(int variant=0;variant<3;variant++)
            for(int phase=0;phase<5;phase++)Get(character,variant,(MeleeSoundPhase)phase);
        Get(0,0,MeleeSoundPhase.Charge);Get(0,0,MeleeSoundPhase.Ready);
    }
    private static AudioStreamWav Render(int character,int variant,MeleeSoundPhase phase)
    {
        const int rate=44100;
        double duration=phase switch
        {
            MeleeSoundPhase.Ignition=>.07,
            MeleeSoundPhase.Swing=>character==2?.34:character==1?.23:.19,
            MeleeSoundPhase.Hit=>character==2?.34:.21,
            MeleeSoundPhase.Miss=>.14,
            MeleeSoundPhase.Critical=>.27,
            MeleeSoundPhase.Charge=>1,
            _=>.18
        };
        int count=(int)(rate*duration);var samples=new double[count*2];
        var random=new Random(7901+character*173+variant*101+(int)phase*307);
        double low=0,slow=0,bodyPhase=0,ringPhase=0,dc=0,peak=.001;
        for(int i=0;i<count;i++)
        {
            double seconds=(double)i/rate,t=(double)i/count;
            double white=random.NextDouble()*2-1;
            low+=.13*(white-low);slow+=.015*(white-slow);
            double high=white-low,band=low-slow;
            double detune=1+(variant-1)*.017;
            double bodyHz=(character==2?58:character==1?175:122)*detune;
            bodyPhase+=Math.Tau*(bodyHz+bodyHz*1.8*Math.Exp(-seconds*48))/rate;
            ringPhase+=Math.Tau*(620+character*180+variant*23)/rate;
            double attack=Math.Min(1,seconds/.004);
            double tail=Math.Pow(1-t,2.2);
            double value;
            switch(phase)
            {
                case MeleeSoundPhase.Ignition:
                    value=(high*.24+band*.65+Math.Sin(bodyPhase)*.10)*attack*Math.Exp(-seconds*64);
                    break;
                case MeleeSoundPhase.Swing:
                    double sweep=Math.Pow(Math.Sin(Math.PI*Math.Pow(t,.64)),1.8);
                    double flame=band*(character==2?1.4:1.1)+high*(character==1?.12:.065);
                    double body=Math.Sin(bodyPhase)*(character==2?.27:.065)*Math.Exp(-seconds*15);
                    value=(flame*sweep+body)*attack*tail;
                    break;
                case MeleeSoundPhase.Hit:
                    double transient=(high*.26+band*.8)*Math.Exp(-seconds*65);
                    double thump=Math.Sin(bodyPhase)*(character==2?.72:.42)*Math.Exp(-seconds*(character==2?16:29));
                    double ember=band*.44*Math.Exp(-seconds*14)+high*.055*Math.Pow(Math.Max(0,Math.Sin(ringPhase)),16)*tail;
                    value=(transient+thump+ember)*attack*tail;
                    break;
                case MeleeSoundPhase.Miss:
                    value=(band*.7+high*.065)*Math.Sin(Math.PI*t)*tail;
                    break;
                case MeleeSoundPhase.Critical:
                    // One electrical snap, a short inharmonic resonance, then sparse discharge.
                    double arc=Math.Exp(-seconds*85)+.25*Math.Exp(-Math.Pow((seconds-.055)*105,2));
                    double glass=(Math.Sin(seconds*2351*Math.Tau)+Math.Sin(seconds*3617*Math.Tau)*.38)*Math.Exp(-seconds*34);
                    value=(high*.62*arc+glass*.16+Math.Sin(bodyPhase)*.18*Math.Exp(-seconds*38))*attack*tail;
                    break;
                case MeleeSoundPhase.Charge:
                    // Integer harmonics make the low body continuous at the one-second seam.
                    value=(Math.Sin(seconds*Math.Tau*110)*.16+Math.Sin(seconds*Math.Tau*221)*.05+band*.30)*(1+.12*Math.Sin(seconds*Math.Tau*7));
                    break;
                default:
                    value=(Math.Sin(seconds*Math.Tau*880)*.19+Math.Sin(seconds*Math.Tau*1320)*.10+high*.16*Math.Exp(-seconds*55))*attack*tail;
                    break;
            }
            dc+=.001*(value-dc);value=Math.Tanh((value-dc)*1.8);
            double pan=phase==MeleeSoundPhase.Swing?(character==1?1:-1)*(.24-.48*t):0;
            samples[i*2]=value*Math.Sqrt((1-pan)*.5);samples[i*2+1]=value*Math.Sqrt((1+pan)*.5);
            peak=Math.Max(peak,Math.Max(Math.Abs(samples[i*2]),Math.Abs(samples[i*2+1])));
        }
        if(phase==MeleeSoundPhase.Charge)
        {
            const int seam=256;
            for(int i=0;i<seam;i++)for(int channel=0;channel<2;channel++)
            {
                int end=(count-seam+i)*2+channel;
                double blend=(double)i/(seam-1);
                samples[end]=samples[end]*(1-blend)+samples[channel]*blend;
            }
        }
        var bytes=new byte[count*4];double gain=.70/Math.Max(.70,peak);
        for(int i=0;i<samples.Length;i++)
        {
            short sample=(short)(Math.Clamp(samples[i]*gain,-.95,.95)*32767);
            bytes[i*2]=(byte)(sample&255);bytes[i*2+1]=(byte)((sample>>8)&255);
        }
        return new AudioStreamWav
        {
            Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Stereo=true,Data=bytes,
            LoopMode=phase==MeleeSoundPhase.Charge?AudioStreamWav.LoopModeEnum.Forward:AudioStreamWav.LoopModeEnum.Disabled,
            LoopBegin=0,LoopEnd=count
        };
    }
    public void Dispose(){foreach(var sound in _cache.Values)sound.Dispose();_cache.Clear();}
}
