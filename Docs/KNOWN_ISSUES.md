# Known Issues and Limitations

No blocker or critical gameplay issue remains in the verified build.

- Movement uses direct deterministic steering with stuck recovery, not obstacle-aware NavMesh paths. The authored layout keeps routes open, but workers may visually pass close to furniture.
- Character motion is lightweight procedural posing/bobbing rather than a skeletal animation set.
- Audio is intentionally restrained: HVAC ambience and small synthesized interaction cues, without a broad Foley library or music.
- The productivity overlay communicates desk quality by color but does not include a persistent legend.
- The release log can contain Unity's benign D3D12 info-queue message and one engine-level missing-Behaviour warning during Office scene construction. Neither produces a missing visible object, exception, failed test, or broken package step.
- The prototype has no save system, localization, remappable controls, or settings menu; these are explicit one-to-two-day prototype non-goals.
