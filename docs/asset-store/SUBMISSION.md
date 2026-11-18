# Asset Store submission checklist

Format: **asset package (.unitypackage)**, uploaded from the Unity **2022.3** project in `AssetStoreUpload/`.
The UPM route needs a separate enrollment and a namespace that Unity assigns, so it is better left for later.

## What is ready in the repo

| Item | Where |
|---|---|
| Store folder (code, sample, docs) | `AssetStoreUpload/Assets/LocaSync Glyph Scanner/`. Rebuild it with `python -I tools/build_asset_store_folder.py` |
| Upload project (Unity 2022.3.62f3, TMP 3.0.9) | `AssetStoreUpload/`. Compiles with 0 errors and 0 warnings |
| Listing copy (title, description, features, technical details, keywords, reviewer notes) | `docs/asset-store/listing.md` |
| Icon 160×160 (no text) | `docs/asset-store/images/icon.png` |
| Card 420×280 | `docs/asset-store/images/card.png` |
| Cover 1950×1300 | `docs/asset-store/images/cover.png` |
| Social media 1200×630 (no text) | `docs/asset-store/images/social.png` |
| Screenshots 1920×1080 | `docs/asset-store/images/shot-results.png`, `shot-setup.png` |

Images are rendered from `docs/asset-store/images-src/*.html` with `bash tools/render_store_images.sh`.

## Status

Done: publisher profile (TinyRig), Publishing Tools installed, draft created and filled in, Validator run, package uploaded from Unity 2022.3.62f3. Left: steps 7 (Preview and Submit) and the support email below. After a code change, rebuild the store folder, upload again with the Uploader (step 6) and update the version and release notes in the draft.

**2026-11-18: version 0.1.5 was submitted and is in review.** Version 0.1.6 (case tags, font tags, sprite assets and emoji fallbacks) is on GitHub only. After 0.1.5 is approved, rebuild the store folder, run the Validator, upload 0.1.6 as an update and set the version and release notes in the portal.

## Steps only you can do (your account)

1. **Publisher profile**: create it at https://publisher.unity.com. Publisher name: **TinyRig**. The card, cover and `package.json` author already use it. LocaSync stays the product line name.
2. In the publisher profile, set a **support email** and fill in `TODO: publisher support email` in `listing.md`.
3. **Asset Store Publishing Tools**: open the [Asset Store page](https://assetstore.unity.com/packages/package/5368745), click *Add to My Assets* and then *Open in Unity*, and import it into `AssetStoreUpload` (Unity 2022.3).
   Don't move it into `Assets/LocaSync Glyph Scanner/`. The guidelines forbid an `AssetStoreTools` folder inside the package.
4. In the Publisher Portal, click **Create a package** to make a draft. Paste the text from `listing.md` and upload the images.

## Steps Claude can do once the tools are installed

5. **Tools → Asset Store → Validator**: add the path `Assets/LocaSync Glyph Scanner`, click Validate, and fix any warnings.
6. **Tools → Asset Store → Uploader**: sign in with your Unity ID (you type the password), select the draft and the `Assets/LocaSync Glyph Scanner` folder, then click **Export and Upload**.

## Final step (you)

7. In the Publisher Portal, click **Preview** to check the page, then **Submit**. Paste "Notes for the reviewer" from `listing.md` into the submit dialog.
   Review takes at least 5 business days. If you hear nothing within 2 weeks, contact Unity support.

## Notes and risks

- **License**: the GitHub version is MIT; the Asset Store version is distributed under the Asset Store EULA. You own the code, so dual distribution is fine. `LICENSE.md` is left out of the store folder.
- **Cover image**: the guidelines allow only the title, publisher and tagline as text. The cover's glyph tiles (A, 가, あ, 中) are illustration. If the review rejects them as text, swap in tofu-box shapes only.
- **Installing both copies**: the store copy and the git (UPM) copy share assembly names and GUIDs. The README says to install only one.
- **2022.3 UI**: compiling and all 89 tests pass on 2022.3, but the scanner window hasn't been opened on screen in 2022.3. Open it once before uploading.
