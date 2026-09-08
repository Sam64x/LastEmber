using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace LastEmber;

// Offline listening renders consume the exact same mix policy as the game.
public static class AudioPreviewPlan
{
    public static void Export()
    {
        var presets=new List<object>();
        Add("01_The_Last_Ember","menu",96,t=>new(MusicCue.Menu,1,0));
        Add("02_Dead_Streets","streets",192,t=>new(MusicCue.Streets,.85f,.05f));
        Add("03_Feed_the_Fire","streets",192,t=>new(MusicCue.Streets,.90f,.80f));
        Add("04_The_Offering","offering",120,t=>new(MusicCue.Offering,.85f,t<60?.1f:.85f,Elite:t>=60));
        Add("05_The_Extinguisher","boss",288,t=>new(MusicCue.Boss,.85f,.95f,t<72?4:t<120?3:t<156?2:t<180?1:0,t<228?.5f:.15f-(t-228)/600,t<228?0:t-228));
        Add("06_The_Furnace_Breathes","victory",48,t=>new(MusicCue.Victory,1,0));
        Add("Flame_Adaptive_Demo","streets",92,t=>new(MusicCue.Streets,t<32?1:t<44?.5f:t<56?.2f:t<68?.05f:t<80?.25f:.8f,t<16?.05f:t<32?.9f:t<68?.8f:.3f));
        var layers=new List<string>();for(int i=0;i<(int)MusicLayer.Count;i++)layers.Add(((MusicLayer)i).ToString().ToLowerInvariant());
        var path=ProjectSettings.GlobalizePath("res://TestResults/Audio");System.IO.Directory.CreateDirectory(path);
        System.IO.File.WriteAllText(System.IO.Path.Combine(path,"mix-presets.json"),JsonSerializer.Serialize(new{controlRate=20,layers,presets}));
        void Add(string name,string cue,float seconds,Func<float,MusicFrame> sample)
        {
            var frames=new List<float[]>();var previous=new float[(int)MusicLayer.Count];
            for(int frame=0;frame<(int)(seconds*20);frame++)
            {
                var target=new float[(int)MusicLayer.Count];MusicMix.Evaluate(sample(frame/20f),target);
                var rendered=new float[target.Length];
                for(int i=0;i<target.Length;i++)
                {
                    previous[i]=Mathf.Lerp(previous[i],target[i],1-Mathf.Exp(-.05f*(target[i]<previous[i]?2.5f:1.3f)));
                    rendered[i]=previous[i]*MusicDirector.Calibration((MusicLayer)i)*.8f*Mathf.Min(1,frame/56f);
                }
                frames.Add(rendered);
            }
            presets.Add(new{name,cue,seconds,frames});
        }
    }
}
