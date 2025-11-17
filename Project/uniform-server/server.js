// server.js
const express = require("express");
const cors = require("cors");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
require("dotenv").config();

// OpenAI SDK (CJS default export)
const OpenAI = require("openai");
const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
  // organization: process.env.OPENAI_ORG_ID, // 필요 시 지정
});

const app = express();
app.use(cors());
app.use(express.json({ limit: "20mb" }));

// 정적 파일 경로
const STATIC_DIR = path.join(__dirname, "static");
const SKINS_DIR  = path.join(STATIC_DIR, "skins");
fs.mkdirSync(SKINS_DIR, { recursive: true });
app.use("/static", express.static(STATIC_DIR));

const PORT = 8000;
const HOST = `http://localhost:${PORT}`;

// 모델/옵션 환경변수화
const IMG_MODEL   = process.env.IMG_MODEL   || "dall-e-3";
const IMG_SIZE    = process.env.IMG_SIZE    || "1024x1024";  // dall-e-3: 1024x1024, 1024x1792, 1792x1024 지원
const IMG_QUALITY = process.env.IMG_QUALITY || "standard";   // "standard" | "hd" (요금 상이)

// 캐시 키(프롬프트+옵션 → 파일명)
function keyOf({ prompt, team, tiling, strength }) {
  const p  = (prompt   || "").trim().toLowerCase();
  const t  = (team     || "").trim().toLowerCase();
  const ti = String(tiling ?? 4);
  const st = String(strength ?? 1);
  return crypto.createHash("sha1").update(`${t}|${p}|${ti}|${st}|${IMG_MODEL}|${IMG_SIZE}|${IMG_QUALITY}`).digest("hex");
}

// OpenAI 이미지 생성 (base64)
async function generateImageBytes(prompt) {
  const fullPrompt = [
    prompt,
    "tileable seamless pattern, sports fabric texture, high-contrast, clean edges, repeating tile"
  ].filter(Boolean).join(", ");

  const resp = await openai.images.generate({
    model: IMG_MODEL,          // "dall-e-3"
    prompt: fullPrompt,
    size: IMG_SIZE,
    quality: IMG_QUALITY,      // "standard" | "hd"
    n: 1,
    response_format: "b64_json"
  });

  // 1) b64_json 경로
  const b64 = resp?.data?.[0]?.b64_json;
  if (b64) return Buffer.from(b64, "base64");

  // 2) url 폴백
  const url = resp?.data?.[0]?.url;
  if (url) {
    const r = await fetch(url);
    if (!r.ok) throw new Error(`download failed: ${r.status}`);
    return Buffer.from(await r.arrayBuffer());
  }

  // 3) 실패
  throw new Error("No image payload (neither b64_json nor url) in response");
}

// Unity 호출 엔드포인트
app.post("/generate", async (req, res) => {
  const startedAt = Date.now();
  try {
    const { prompt, team, tiling, strength } = req.body || {};
    if (!process.env.OPENAI_API_KEY) {
      return res.status(500).json({ error: "OPENAI_API_KEY missing" });
    }
    if (!prompt || !team) {
      return res.status(400).json({ error: "missing prompt/team" });
    }

    const key     = keyOf({ prompt, team, tiling, strength });
    const outPath = path.join(SKINS_DIR, `${key}.png`);
    const outUrl  = `${HOST}/static/skins/${key}.png`;

    // 1) 캐시
    if (fs.existsSync(outPath)) {
      console.log(`[CACHE HIT] ${team} • "${prompt}" → ${outUrl} (${Date.now()-startedAt}ms)`);
      return res.json({ image_url: outUrl });
    }

    // 2) 생성
    let img = null, lastErr = null;
    for (let attempt = 1; attempt <= 2; attempt++) {
      try {
        console.log(`[OPENAI] attempt=${attempt} model=${IMG_MODEL} team=${team} prompt="${prompt}"`);
        img = await generateImageBytes(prompt);
        break;
      } catch (e) {
        lastErr = e;
        console.warn(`[OPENAI] attempt ${attempt} failed:`, e?.status || "", e?.message || e);
        // gpt-image-1의 403(조직 미인증)만 별도 메시지
        if (e?.status === 403 && /must be verified/i.test(e?.message || "")) {
          return res.status(403).json({
            error: "org_verification_required",
            detail: "gpt-image-1 requires organization verification. Use dall-e-3 or complete verification."
          });
        }
        await new Promise(r => setTimeout(r, 600));
      }
    }
    if (!img) throw lastErr || new Error("image generation failed");

    // 3) 저장
    fs.writeFileSync(outPath, img);
    console.log(`[SAVED] ${outPath} (${Date.now()-startedAt}ms)`);

    // 4) URL 반환
    return res.json({ image_url: outUrl });
  } catch (e) {
    console.error("[SERVER ERROR]", e?.message, e?.response?.data || "", e?.stack || "");
    res.status(500).json({ error: "server_error", detail: String(e?.message || e) });
  }
});

app.listen(PORT, () => console.log(`Uniform server running at ${HOST}`));