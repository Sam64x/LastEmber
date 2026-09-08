using System;
using Godot;

namespace LastEmber;

public enum MusicCue { Menu, Streets, Offering, Boss, Victory, Silence }
public enum MusicLayer { Ambient, Melody, Strings, Percussion, Bass, Choir, Tension, Heartbeat, Breath, Fire, Fragments, Hope, Count }
public readonly record struct MusicFrame(MusicCue Cue, float Flame, float Danger, int LitTorches=4, float BossHealth=1, float HopeSeconds=0, bool Elite=false);

// Pure mix policy, separate from playback. Gains are linear; fades happen in the director.
public static class MusicMix
{
    private static float S(float low,float high,float value)=>Mathf.SmoothStep(low,high,value);
    public static void Evaluate(MusicFrame state,Span<float> gains)
    {
        gains.Clear();
        float flame=Mathf.Clamp(state.Flame,0,1),danger=Mathf.Clamp(state.Danger,0,1);
        if(state.Cue==MusicCue.Silence)return;
        if(state.Cue==MusicCue.Menu)
        {
            gains[(int)MusicLayer.Ambient]=.38f;
            gains[(int)MusicLayer.Melody]=.88f;
            gains[(int)MusicLayer.Fire]=.28f;
            return;
        }
        if(state.Cue==MusicCue.Victory)
        {
            gains[(int)MusicLayer.Ambient]=.30f;gains[(int)MusicLayer.Melody]=.94f;
            gains[(int)MusicLayer.Strings]=.52f;gains[(int)MusicLayer.Choir]=.30f;gains[(int)MusicLayer.Fire]=.38f;
            return;
        }
        float life=S(.045f,.48f,flame),warmth=S(.36f,.9f,flame),critical=1-S(.07f,.30f,flame);
        float low=1-S(.17f,.43f,flame);
        gains[(int)MusicLayer.Ambient]=.025f+.31f*life;
        gains[(int)MusicLayer.Melody]=.54f*life;
        gains[(int)MusicLayer.Strings]=.38f*warmth;
        gains[(int)MusicLayer.Percussion]=.72f*S(.18f,.8f,danger)*S(.18f,.72f,flame);
        gains[(int)MusicLayer.Bass]=.49f*S(.23f,.84f,danger)*S(.09f,.48f,flame);
        gains[(int)MusicLayer.Fire]=.035f+.45f*S(.06f,.82f,flame);
        gains[(int)MusicLayer.Heartbeat]=low*(.30f+.20f*danger);
        gains[(int)MusicLayer.Breath]=critical*(.16f+.13f*danger);
        gains[(int)MusicLayer.Fragments]=.30f*low*S(.055f,.17f,flame);
        // A large flame invites attention: distant high textures even outside combat.
        gains[(int)MusicLayer.Tension]=.25f*S(.78f,1,flame)+.12f*danger*life;
        if(state.Cue==MusicCue.Offering)
        {
            gains[(int)MusicLayer.Choir]=.60f*life;
            gains[(int)MusicLayer.Strings]=.22f*warmth;
            gains[(int)MusicLayer.Melody]=.32f*life;
            gains[(int)MusicLayer.Percussion]=state.Elite?.74f*life:0;
        }
        if(state.Cue!=MusicCue.Boss)return;
        // The world loses its instruments one torch at a time.
        int torches=Math.Clamp(state.LitTorches,0,4);
        gains[(int)MusicLayer.Choir]=torches==4?.62f*life:0;
        gains[(int)MusicLayer.Strings]=torches>=3?.70f*life:0;
        gains[(int)MusicLayer.Percussion]=torches>=2?.84f*life:torches==1?.08f*life:0;
        gains[(int)MusicLayer.Bass]=torches>0?.58f*life:0;
        gains[(int)MusicLayer.Melody]=torches>0?.52f*life:0;
        gains[(int)MusicLayer.Tension]=torches>0?.18f*life:0;
        if(torches==0)
        {
            gains[(int)MusicLayer.Ambient]=.045f;
            gains[(int)MusicLayer.Heartbeat]=.43f;
            gains[(int)MusicLayer.Breath]=.25f;
            gains[(int)MusicLayer.Fire]=.10f*Mathf.Max(.18f,flame);
            gains[(int)MusicLayer.Fragments]=0;
        }
        if(state.BossHealth<=.15f)
        {
            float inner=.35f+.65f*S(.03f,.4f,flame);
            gains[(int)MusicLayer.Hope]=.9f*inner;
            gains[(int)MusicLayer.Strings]=.66f*inner*Mathf.Max(S(1,4,state.HopeSeconds),1-S(.09f,.15f,state.BossHealth));
            gains[(int)MusicLayer.Percussion]=.76f*inner*Mathf.Max(S(3,7,state.HopeSeconds),1-S(.03f,.09f,state.BossHealth));
            gains[(int)MusicLayer.Bass]=.38f*inner*Mathf.Max(S(3,8,state.HopeSeconds),1-S(.03f,.08f,state.BossHealth));
            gains[(int)MusicLayer.Heartbeat]*=.60f;
            gains[(int)MusicLayer.Breath]*=.5f;
        }
    }
}
