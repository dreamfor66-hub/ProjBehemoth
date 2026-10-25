# DOS Story

- Requested reference: [도스이야기 — 눈누](https://noonnu.cc/font_page/1140)
- Exact referenced web font: [DOSIyagiMedium.woff2](https://cdn.jsdelivr.net/gh/projectnoonnu/noonfonts_2304-2@1.0/DOSIyagiMedium.woff2)
- Author and original collection: [Damheo Lee / leedheo — hurss/fonts](https://github.com/hurss/fonts)
- License: accompanying [MIT license](LICENSE.txt).

The WOFF2 container was decompressed to TTF with fontTools for Unity import. Its internal family name is `DOSIyagiBoldface`; this is the font served as `DOSIyagiMedium` by the supplied reference. The glyph design was retained.

Attribution: 글꼴(폰트)로 도스이야기(leedheo 제작)를 사용하였습니다.

## Project mixed source

`DOSStoryGothicLatin.ttf` is a separately named derivative: original DOS Story glyphs and metrics,
with ASCII English letters A–Z/a–z and digits 0–9 from [DOS Gothic](../DosGothic/SOURCE.md).
Both sources use the same 1024-unit em, ascent/descent and 16 px native grid. Other characters,
including Korean, punctuation and symbols, retain their original DOS Story mapping and outlines.
The original `DOSIyagiMedium.ttf` is retained unmodified.

`Assets/Editor/Presentation/build_typography_assets.py` regenerates the mixed source and the existing
DosStory TMP asset. Keeping a real source font lets TMP add new glyphs dynamically with the same mix.
There are no injected font tags or changes to localized content.

Attribution: 글꼴(폰트)로 도스이야기 및 도스고딕(leedheo 제작)을 사용하였습니다.
