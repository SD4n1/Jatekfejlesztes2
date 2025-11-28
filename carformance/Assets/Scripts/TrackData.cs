using UnityEngine;

[CreateAssetMenu(fileName = "NewTrack", menuName = "Racing/Track Data")]
public class TrackData : ScriptableObject
{
    public string trackName;
    public string sceneName;
    public Texture trackPreviewImage;
}