# I5 diagnostic image markers

These eight original procedural PNGs are development targets, not historical illustrations, QR codes, approved final exhibit signage or evidence of successful device tracking. No external image or museum media is redistributed.

## Files and dimensions

All images are under `ComputerScienceARMuseum/Assets/IARA/Content/Markers/`. Each source is **512 × 512 pixels**, square. The complete image, including its outer edge, must occupy **150 × 150 mm** on paper. The reference library records **0.15 × 0.15 metres**; this is the image area, not the paper or caption size.

| Exhibit | Image filename / reference-library name |
| --- | --- |
| ENC-01 | `marker_enigma.png` / `marker_enigma` |
| ENC-02 | `marker_bombe.png` / `marker_bombe` |
| EAR-01 | `marker_colossus.png` / `marker_colossus` |
| EAR-02 | `marker_eniac.png` / `marker_eniac` |
| PC-01 | `marker_apple_i.png` / `marker_apple_i` |
| PC-02 | `marker_apple_ii.png` / `marker_apple_ii` |
| PC-03 | `marker_ibm_5150.png` / `marker_ibm_5150` |
| PC-04 | `marker_macintosh_128k.png` / `marker_macintosh_128k` |

Use the filename and catalog ID to label prints outside the tracked square; no ID is encoded as machine-readable text inside these images. Keep the original orientation for repeatable pose checks. The future model orientation relative to a print remains an asset-design decision.

## Printing and measurement

1. Print each complete PNG at exactly 150 mm width and height on flat matte paper. Disable fit-to-page, cropping, borderless enlargement and nonuniform stretching.
2. Keep captions, logos, folds and page decorations outside the tracked square. Do not laminate with reflective material or edit the pattern.
3. Measure the printed square with a ruler. A different print width requires a matching reference-library physical width; a PDF viewer's nominal scale is insufficient evidence.
4. Start future hardware trials with even lighting, a flat unobstructed target, and one print at a time. Record print dimensions, camera distance/angle, illumination, device/OS and reacquisition timing.
5. Test wrong targets, multiple distinct prints and duplicate copies separately. Application policy permits one selected visualization, regardless of the provider's internal trackable count.

512 pixels across 150 mm is approximately 87 source pixels per inch. A high-resolution printer does not add source detail. Re-evaluate print quality, distance and marker design on the target phone before approving final signage. No range, accuracy or performance envelope has been measured.

## Offline tooling evidence

On 2026-11-20, the installed ARCore 6.3.5 package's Windows `arcoreimg` tool evaluated each PNG at **100/100**. A local eight-image database with width 0.15 m was generated successfully under ignored `Library/I5/diagnostic.imgdb`. This is an image-database conversion, **not an Android application build**. The database is verification output; Unity's installed ARCore build processing remains responsible for packaging provider data in a future authorized app build.

[Google's tool documentation](https://developers.google.com/ar/develop/augmented-images/arcoreimg) recommends a score of at least 75. A score measures image features and does not prove detection, pose quality, distinction between prints, tracking-loss behavior or offline device operation. Those require physical testing, which was not performed in I5.

Reference library: `Assets/IARA/AR/Assets/MuseumImages.asset`. Asset-reference registry: `Assets/IARA/AR/Assets/ImageTrackingAssets.asset`. Images and references are bundled locally; no runtime download or web request is introduced. ARCore/device prerequisites must be provisioned before offline use; automatic AR installation/update is disabled in this scene.

Generation is explicit through `IARA > AR > Build tracking foundation (empty scene only)`. The builder refuses existing output assets or an authored nonempty AR scene, and only verifies an already generated scene. It does not regenerate over edited markers. Any future marker replacement needs review, new feature evaluation and physical validation.
