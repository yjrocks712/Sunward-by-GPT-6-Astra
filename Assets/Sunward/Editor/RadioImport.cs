using UnityEditor;
using UnityEngine;

namespace Sunward.Editor {
public sealed class RadioImport:AssetPostprocessor {
 void OnPreprocessAudio(){if(!assetPath.StartsWith("Assets/Sunward/Audio/Radio/"))return;var importer=(AudioImporter)assetImporter;var s=importer.defaultSampleSettings;s.loadType=AudioClipLoadType.Streaming;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.8f;s.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;s.preloadAudioData=false;importer.defaultSampleSettings=s;importer.forceToMono=false;importer.loadInBackground=true;}
}
}
