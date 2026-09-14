using System;
using UnityEngine;

namespace Sunward {
[CreateAssetMenu(menuName="Sunward/Radio library")]
public sealed class RadioLibrary:ScriptableObject {
 [Serializable] public sealed class Track {public string title,artist;public AudioClip clip;}
 [Serializable] public sealed class Station {public string name,frequency,tagline;public Color color=Color.white;public Track[] tracks;}
 public Station[] stations;
}
}
