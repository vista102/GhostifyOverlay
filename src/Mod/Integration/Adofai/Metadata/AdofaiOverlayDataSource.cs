using GhostifyOverlay.Application.Overlay;
using UnityEngine;

namespace DonQuixoteOverlay
{
    internal sealed class AdofaiOverlayDataSource : IOverlayMetricsSource, IOverlayMetadataSource
    {
        public string ReadSong() => OverlayMetadata.CurrentSong(scrController.instance);
        public string ReadTiming() => OverlayMetadata.CurrentTiming(scrController.instance);
        public OverlayTempo ReadTempo()
        {
            float theoretical = 0f, current = 0f, pitch = 1f;
            try
            {
                scrController controller = scrController.instance;
                pitch = ADOBase.conductor.song.pitch;
                theoretical = (float)(ADOBase.conductor.bpm * pitch * controller.playerOne.planetarySystem.speed);
                scrFloor floor = controller.currFloor;
                current = floor != null && floor.nextfloor != null
                    ? (float)(60d / (floor.nextfloor.entryTime - floor.entryTime) * pitch) : theoretical;
            }
            catch { }
            return new OverlayTempo(theoretical, current, pitch);
        }
        public OverlayTimes ReadMusicTimes()
        {
            int current = 0, total = 0;
            try
            {
                AudioSource source = ADOBase.conductor.song;
                current = Mathf.Max(0, Mathf.FloorToInt(source.time));
                total = Mathf.Max(0, Mathf.FloorToInt(source.clip == null ? 0f : source.clip.length));
            }
            catch { }
            return new OverlayTimes(current, total);
        }
        public OverlayTimes ReadMapTimes()
        {
            int current = 0, total = 0;
            try
            {
                scrController controller = scrController.instance;
                int last = ADOBase.lm.listFloors.Count - 1;
                total = Mathf.Max(0, Mathf.FloorToInt((float)(ADOBase.lm.listFloors[last].entryTime - ADOBase.lm.listFloors[1].entryTime)));
                current = Mathf.Max(0, Mathf.FloorToInt((float)(controller.currFloor == null ? 0 : controller.currFloor.entryTime - ADOBase.lm.listFloors[1].entryTime)));
            }
            catch { }
            return new OverlayTimes(current, total);
        }
    }
}
