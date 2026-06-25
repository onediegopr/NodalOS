export class VisualCaptchaSolver {
  constructor(config = {}) {
    this.enabled = config.enabled || false;
    this.ocrEngine = config.ocrEngine || 'tesseract';
    this.aiVision = config.aiVision || { enabled: false, provider: 'openai', apiKey: '', model: 'gpt-4-vision-preview' };
    this.tesseractReady = false;
    this._initOcr();
  }

  async _initOcr() {
    if (!this.enabled) return;
    try {
      const Tesseract = (await import('tesseract.js')).default;
      this._tesseract = Tesseract;
      this.tesseractReady = true;
      console.log('[VisualCaptchaSolver] Tesseract OCR ready');
    } catch (e) {
      console.warn('[VisualCaptchaSolver] Tesseract.js not available, OCR disabled:', e.message);
      this.tesseractReady = false;
    }
  }

  async classify(imageBuffer) {
    if (!this.enabled) return 'unknown';
    try {
      const sharp = (await import('sharp')).default;
      const metadata = await sharp(imageBuffer).metadata();
      const { width, height } = metadata;

      const resized = await sharp(imageBuffer)
        .resize(Math.min(width, 300), Math.min(height, 300), { fit: 'inside' })
        .greyscale()
        .raw()
        .toBuffer();

      const totalPixels = resized.length;
      let darkPixels = 0;
      let runs = 0;
      let inRun = false;
      for (let i = 0; i < totalPixels; i++) {
        if (resized[i] < 128) { darkPixels++; if (!inRun) { runs++; inRun = true; } }
        else { inRun = false; }
      }

      const darkRatio = darkPixels / totalPixels;
      const runDensity = runs / Math.max(totalPixels / 100, 1);

      if (darkRatio > 0.1 && darkRatio < 0.5 && runDensity > 0.5) return 'text-distorted';
      if (darkRatio > 0.3 && darkRatio < 0.8 && runDensity > 1.0) return 'image-selection';
      if (darkRatio < 0.15 && runDensity < 0.3) return 'puzzle-slider';
      return 'unknown';
    } catch (e) {
      if (e.code === 'ERR_MODULE_NOT_FOUND') return 'unknown';
      console.warn('[VisualCaptchaSolver] classify error:', e.message);
      return 'unknown';
    }
  }

  async solve(imageBuffer, type, options = {}) {
    if (!this.enabled) return { success: false, error: 'VisualCaptchaSolver not enabled' };

    switch (type) {
      case 'text-distorted': return this._solveText(imageBuffer);
      case 'image-selection': return this._solveImageSelection(imageBuffer, options);
      case 'puzzle-slider': return this._solvePuzzleSlider(imageBuffer);
      case 'unknown': return this._solveWithAI(imageBuffer, options);
      default: return this._solveWithAI(imageBuffer, options);
    }
  }

  async _solveText(imageBuffer) {
    if (!this.tesseractReady) return this._solveWithAI(imageBuffer, { prompt: 'Extract only the distorted text from this CAPTCHA image. Return only the text, nothing else.' });

    try {
      const sharp = (await import('sharp')).default;
      const processed = await sharp(imageBuffer)
        .resize(300, 100, { fit: 'inside' })
        .greyscale()
        .normalize()
        .linear(1.5, -40)
        .sharpen()
        .toBuffer();

      const { data: { text } } = await this._tesseract.recognize(processed, 'eng', {
        tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789',
      });

      const clean = (text || '').replace(/[^a-zA-Z0-9]/g, '').trim();
      if (clean.length >= 3) {
        console.log('[VisualCaptchaSolver] OCR extracted: ' + clean);
        return { success: true, answer: clean, method: 'ocr' };
      }
      return { success: false, error: 'OCR produced insufficient text' };
    } catch (e) {
      console.warn('[VisualCaptchaSolver] OCR error:', e.message);
      return { success: false, error: 'OCR failed: ' + e.message };
    }
  }

  async _solveImageSelection(imageBuffer, options = {}) {
    const objective = options.objective || 'the requested object';
    return this._solveWithAI(imageBuffer, {
      prompt: `This is a CAPTCHA image. The question asks to select images containing ${objective}. Analyze the grid of images and return ONLY the 1-based index numbers of the images you would select, separated by commas. Example: "1,3,4"`,
    });
  }

  async _solvePuzzleSlider(imageBuffer) {
    try {
      const sharp = (await import('sharp')).default;
      const processed = await sharp(imageBuffer).greyscale().normalize().raw().toBuffer();
      const row = Math.floor(processed.length / 2);
      const rowData = processed.slice(row, row + processed.length);
      let gapStart = -1, gapEnd = -1;
      for (let i = 0; i < rowData.length; i++) {
        if (rowData[i] > 180 && gapStart < 0) gapStart = i;
        else if (rowData[i] < 80 && gapStart >= 0 && gapEnd < 0) { gapEnd = i; break; }
      }
      if (gapStart >= 0 && gapEnd >= gapStart) {
        const distance = gapStart;
        return { success: true, answer: distance, method: 'pixel-scan' };
      }
      return { success: false, error: 'Could not detect slider gap' };
    } catch (e) {
      return { success: false, error: 'Puzzle detection failed: ' + e.message };
    }
  }

  async _solveWithAI(imageBuffer, options = {}) {
    if (!this.aiVision.enabled || !this.aiVision.apiKey) {
      return { success: false, error: 'AI vision not configured' };
    }

    const prompt = options.prompt || 'Solve this CAPTCHA. Return only the answer, nothing else.';

    try {
      const base64 = imageBuffer.toString('base64');
      const provider = this.aiVision.provider || 'openai';

      if (provider === 'openai') {
        const resp = await fetch('https://api.openai.com/v1/chat/completions', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + this.aiVision.apiKey },
          body: JSON.stringify({
            model: this.aiVision.model || 'gpt-4-vision-preview',
            messages: [{
              role: 'user',
              content: [
                { type: 'text', text: prompt },
                { type: 'image_url', image_url: { url: 'data:image/png;base64,' + base64, detail: 'low' } },
              ],
            }],
            max_tokens: 50,
            temperature: 0,
          }),
        });

        const data = await resp.json();
        if (data.choices && data.choices[0]) {
          const answer = data.choices[0].message.content.trim();
          console.log('[VisualCaptchaSolver] AI answered: ' + answer);
          return { success: true, answer, method: 'gpt-4v' };
        }
        return { success: false, error: 'AI returned no answer' };
      }

      return { success: false, error: 'Unsupported AI provider: ' + provider };
    } catch (e) {
      console.warn('[VisualCaptchaSolver] AI error:', e.message);
      return { success: false, error: 'AI vision failed: ' + e.message };
    }
  }
}
