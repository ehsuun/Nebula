using UnityEngine;
using UnityEngine.Playables;

public class AudioReactiveClip : PlayableAsset
{
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<AudioReactiveBehaviour>.Create(graph);
        return playable;
    }
}
