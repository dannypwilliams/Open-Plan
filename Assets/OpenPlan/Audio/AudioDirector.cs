using UnityEngine;

namespace OpenPlan
{
    public sealed class AudioDirector : MonoBehaviour
    {
        private AudioSource ambience;
        private AudioSource oneShot;
        private AudioClip confirm;
        private AudioClip taskDone;
        private AudioClip placementSuccess;
        private AudioClip placementRejected;
        private bool suppressNextNoticeCue;
        public string LastCue { get; private set; }

        public void Initialize(OfficeDirector office)
        {
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.volume = .055f;
            ambience.clip = BuildNoise("Office HVAC", 6f, .018f, 71);
            ambience.Play();
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.volume = .22f;
            confirm = BuildTone("Paper stamp", .13f, 150f, 92f);
            taskDone = BuildTone("Task complete", .34f, 520f, 760f);
            placementSuccess = BuildTone("Placement success", .16f, 360f, 520f);
            placementRejected = BuildTone("Placement rejected", .18f, 210f, 145f);
            office.Tasks.TaskCompleted += _ => oneShot.PlayOneShot(taskDone);
            office.Notice += _ =>
            {
                if (suppressNextNoticeCue) { suppressNextNoticeCue = false; return; }
                if (oneShot != null) oneShot.PlayOneShot(confirm, .55f);
            };
        }

        public void PlayPlacementSuccess()
        {
            LastCue = "placement-success";
            if (oneShot != null) oneShot.PlayOneShot(placementSuccess, .72f);
        }

        public void PlayPlacementRejected()
        {
            LastCue = "placement-rejected";
            suppressNextNoticeCue = true;
            if (oneShot != null) oneShot.PlayOneShot(placementRejected, .64f);
        }

        private static AudioClip BuildNoise(string name, float length, float amplitude, int seed)
        {
            const int rate = 22050;
            float[] data = new float[Mathf.CeilToInt(length * rate)];
            System.Random random = new System.Random(seed);
            float filtered = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                filtered = Mathf.Lerp(filtered, (float)(random.NextDouble() * 2.0 - 1.0), .015f);
                data[i] = filtered * amplitude;
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildTone(string name, float length, float startFrequency, float endFrequency)
        {
            const int rate = 22050;
            int samples = Mathf.CeilToInt(length * rate);
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency * Mathf.PI * 2f / rate;
                data[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * .18f;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
