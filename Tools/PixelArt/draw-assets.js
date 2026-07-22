#!/usr/bin/env node

// Deterministic pixel artwork authored for Pixelorama/Piskel.
// The script writes layered .piskel source files. Pixelorama performs the
// production PNG export, keeping the editable pixel-art source in the repo.

const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const workspace = path.resolve(__dirname, "../..");
const sourceDir = path.join(workspace, "Art", "Pixelorama");
fs.mkdirSync(sourceDir, { recursive: true });

const PALETTE = {
  transparent: [0, 0, 0, 0],
  void: [11, 13, 20, 255],
  shadow: [20, 19, 27, 255],
  ink: [35, 25, 29, 255],
  wallDark: [59, 42, 45, 255],
  wall: [96, 67, 69, 255],
  wallLight: [137, 102, 99, 255],
  woodDark: [64, 47, 50, 255],
  wood: [79, 59, 56, 255],
  tileDark: [56, 70, 78, 255],
  tile: [76, 93, 101, 255],
  teal: [49, 81, 88, 255],
  tealLight: [69, 105, 110, 255],
  cream: [184, 162, 139, 255],
  amber: [210, 168, 92, 255],
  rust: [123, 54, 63, 255],
  skin: [215, 170, 130, 255],
};

function image(width, height, fill = PALETTE.transparent) {
  const data = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i += 1) {
    data.set(fill, i * 4);
  }
  return { width, height, data };
}

function put(img, x, y, color) {
  if (x < 0 || y < 0 || x >= img.width || y >= img.height) return;
  img.data.set(color, (y * img.width + x) * 4);
}

function fillRect(img, x, y, width, height, color) {
  for (let py = y; py < y + height; py += 1) {
    for (let px = x; px < x + width; px += 1) put(img, px, py, color);
  }
}

function strokeRect(img, x, y, width, height, color, thickness = 1) {
  fillRect(img, x, y, width, thickness, color);
  fillRect(img, x, y + height - thickness, width, thickness, color);
  fillRect(img, x, y, thickness, height, color);
  fillRect(img, x + width - thickness, y, thickness, height, color);
}

function roundedRect(img, x, y, width, height, fill, outline = PALETTE.ink) {
  fillRect(img, x + 1, y, width - 2, height, outline);
  fillRect(img, x, y + 1, width, height - 2, outline);
  fillRect(img, x + 2, y + 1, width - 4, height - 2, fill);
  fillRect(img, x + 1, y + 2, width - 2, height - 4, fill);
}

function line(img, x0, y0, x1, y1, color) {
  let dx = Math.abs(x1 - x0);
  let sx = x0 < x1 ? 1 : -1;
  let dy = -Math.abs(y1 - y0);
  let sy = y0 < y1 ? 1 : -1;
  let error = dx + dy;
  while (true) {
    put(img, x0, y0, color);
    if (x0 === x1 && y0 === y1) break;
    const doubled = error * 2;
    if (doubled >= dy) {
      error += dy;
      x0 += sx;
    }
    if (doubled <= dx) {
      error += dx;
      y0 += sy;
    }
  }
}

function fillEllipse(img, cx, cy, rx, ry, color) {
  for (let y = -ry; y <= ry; y += 1) {
    const span = Math.floor(rx * Math.sqrt(Math.max(0, 1 - (y * y) / (ry * ry))));
    fillRect(img, cx - span, cy + y, span * 2 + 1, 1, color);
  }
}

function outlinedEllipse(img, cx, cy, rx, ry, fill, outline = PALETTE.ink) {
  fillEllipse(img, cx, cy, rx, ry, outline);
  if (rx > 2 && ry > 2) fillEllipse(img, cx, cy, rx - 2, ry - 2, fill);
}

function woodFloor(img, x, y, width, height) {
  fillRect(img, x, y, width, height, PALETTE.woodDark);
  for (let py = y + 6; py < y + height; py += 7) {
    line(img, x, py, x + width - 1, py, PALETTE.ink);
  }
  for (let row = 0, py = y; py < y + height; row += 1, py += 7) {
    const offset = row % 2 === 0 ? 16 : 3;
    for (let px = x + offset; px < x + width; px += 27) {
      line(img, px, py, px, Math.min(py + 6, y + height - 1), PALETTE.wood);
    }
  }
}

function checkerFloor(img, x, y, width, height) {
  const tileSize = 10;
  for (let py = y; py < y + height; py += tileSize) {
    for (let px = x; px < x + width; px += tileSize) {
      const odd = ((px - x) / tileSize + (py - y) / tileSize) % 2 === 1;
      fillRect(img, px, py, Math.min(tileSize, x + width - px), Math.min(tileSize, y + height - py), odd ? PALETTE.tile : PALETTE.cream);
    }
  }
}

function tiledFloor(img, x, y, width, height) {
  fillRect(img, x, y, width, height, PALETTE.tileDark);
  for (let py = y; py < y + height; py += 10) line(img, x, py, x + width - 1, py, PALETTE.ink);
  for (let px = x; px < x + width; px += 10) line(img, px, y, px, y + height - 1, PALETTE.ink);
}

function wallSegment(img, x, y, width, height) {
  fillRect(img, x, y, width, height, PALETTE.ink);
  fillRect(img, x + 1, y + 1, width - 2, height - 2, PALETTE.wallDark);
  if (width >= height) fillRect(img, x + 2, y + 1, width - 4, 2, PALETTE.wallLight);
  else fillRect(img, x + 1, y + 2, 2, height - 4, PALETTE.wallLight);
}

function buildApartmentLayers() {
  const width = 320;
  const height = 180;
  const background = image(width, height, PALETTE.void);
  const floors = image(width, height);
  const walls = image(width, height);
  const furniture = image(width, height);
  const accents = image(width, height);

  fillRect(background, 10, 9, 300, 160, PALETTE.shadow);
  fillRect(background, 14, 13, 292, 154, PALETTE.ink);

  woodFloor(floors, 18, 16, 116, 68);
  woodFloor(floors, 140, 16, 40, 146);
  woodFloor(floors, 186, 16, 116, 68);
  checkerFloor(floors, 18, 90, 116, 72);
  tiledFloor(floors, 186, 90, 116, 72);

  // Outer walls. The visual entrance is centered at the bottom, but collision
  // remains closed in the prototype so the player cannot leave the canvas.
  wallSegment(walls, 12, 10, 296, 6);
  wallSegment(walls, 12, 10, 6, 158);
  wallSegment(walls, 302, 10, 6, 158);
  wallSegment(walls, 12, 162, 142, 6);
  wallSegment(walls, 166, 162, 142, 6);
  fillRect(walls, 154, 163, 12, 3, PALETTE.cream);

  // Central hallway walls with four clean door gaps.
  wallSegment(walls, 134, 10, 6, 33);
  wallSegment(walls, 134, 55, 6, 52);
  wallSegment(walls, 134, 119, 6, 49);
  wallSegment(walls, 180, 10, 6, 33);
  wallSegment(walls, 180, 55, 6, 52);
  wallSegment(walls, 180, 119, 6, 49);
  wallSegment(walls, 12, 84, 122, 6);
  wallSegment(walls, 186, 84, 122, 6);

  // Door thresholds make the passable openings obvious without drawing doors.
  for (const y of [43, 107]) {
    fillRect(accents, 134, y, 6, 12, PALETTE.wall);
    fillRect(accents, 180, y, 6, 12, PALETTE.wall);
  }

  // Living room: sofa, coffee table and a low cabinet. Every object is strict
  // top-down geometry, aligned to the wall grid.
  roundedRect(furniture, 27, 24, 48, 17, PALETTE.teal);
  fillRect(furniture, 31, 28, 18, 9, PALETTE.tealLight);
  fillRect(furniture, 52, 28, 18, 9, PALETTE.tealLight);
  line(furniture, 50, 25, 50, 40, PALETTE.ink);
  roundedRect(furniture, 48, 53, 34, 16, PALETTE.wood);
  fillRect(furniture, 53, 57, 24, 7, PALETTE.woodDark);
  roundedRect(furniture, 113, 26, 11, 28, PALETTE.wallDark);

  // Bedroom: compact single bed and square bedside cabinet.
  roundedRect(furniture, 250, 23, 36, 48, PALETTE.wallDark);
  fillRect(furniture, 254, 27, 28, 38, PALETTE.teal);
  fillRect(furniture, 257, 28, 22, 9, PALETTE.cream);
  line(furniture, 254, 39, 281, 39, PALETTE.ink);
  roundedRect(furniture, 226, 26, 15, 15, PALETTE.wood);
  put(furniture, 233, 33, PALETTE.amber);

  // Kitchen: one straight counter, a sink and one small table.
  roundedRect(furniture, 24, 99, 90, 16, PALETTE.wallDark);
  for (const px of [46, 68, 90]) line(furniture, px, 101, px, 112, PALETTE.ink);
  roundedRect(furniture, 28, 102, 17, 10, PALETTE.tileDark);
  fillRect(furniture, 33, 105, 7, 4, PALETTE.ink);
  outlinedEllipse(furniture, 68, 138, 18, 12, PALETTE.wood);
  fillRect(furniture, 66, 149, 4, 7, PALETTE.ink);

  // Bathroom: a clearly readable tub and toilet, both strict top-down.
  roundedRect(furniture, 252, 99, 35, 49, PALETTE.cream);
  roundedRect(furniture, 257, 104, 25, 38, PALETTE.tileDark);
  put(furniture, 276, 136, PALETTE.ink);
  outlinedEllipse(furniture, 217, 136, 10, 14, PALETTE.cream);
  fillRect(furniture, 211, 121, 12, 7, PALETTE.cream);
  strokeRect(furniture, 211, 121, 12, 7, PALETTE.ink);

  // Minimal horror accents: a cold runner and a single displaced red thread.
  roundedRect(accents, 150, 67, 20, 48, PALETTE.teal);
  for (let py = 72; py < 111; py += 6) fillRect(accents, 153, py, 14, 1, PALETTE.tealLight);
  line(accents, 102, 72, 112, 78, PALETTE.rust);
  line(accents, 112, 78, 106, 80, PALETTE.rust);

  return { width, height, layers: [background, floors, walls, furniture, accents] };
}

function buildPlayerLayers() {
  const width = 32;
  const height = 40;
  const sprite = image(width, height);

  // Hair and head outline.
  fillRect(sprite, 11, 3, 10, 2, PALETTE.ink);
  fillRect(sprite, 8, 5, 16, 3, PALETTE.ink);
  fillRect(sprite, 6, 8, 20, 8, PALETTE.ink);
  fillRect(sprite, 8, 16, 16, 3, PALETTE.ink);
  fillRect(sprite, 9, 8, 14, 9, PALETTE.skin);
  fillRect(sprite, 7, 9, 2, 5, PALETTE.skin);
  fillRect(sprite, 23, 9, 2, 5, PALETTE.skin);

  // Blocky hair fringe.
  fillRect(sprite, 8, 5, 16, 5, PALETTE.woodDark);
  fillRect(sprite, 6, 8, 5, 4, PALETTE.woodDark);
  fillRect(sprite, 21, 8, 5, 4, PALETTE.woodDark);
  fillRect(sprite, 10, 9, 3, 2, PALETTE.woodDark);
  fillRect(sprite, 18, 9, 4, 2, PALETTE.woodDark);

  // Exactly two dot eyes. No eyebrows, mouth, nose or expression marks.
  put(sprite, 12, 13, PALETTE.ink);
  put(sprite, 19, 13, PALETTE.ink);

  // Symmetrical coat and resting mitten hands. This is one static frame.
  fillRect(sprite, 9, 18, 14, 14, PALETTE.ink);
  fillRect(sprite, 7, 20, 3, 10, PALETTE.ink);
  fillRect(sprite, 22, 20, 3, 10, PALETTE.ink);
  fillRect(sprite, 10, 19, 12, 12, PALETTE.teal);
  fillRect(sprite, 8, 21, 2, 7, PALETTE.teal);
  fillRect(sprite, 22, 21, 2, 7, PALETTE.teal);
  fillRect(sprite, 8, 28, 2, 2, PALETTE.skin);
  fillRect(sprite, 22, 28, 2, 2, PALETTE.skin);
  fillRect(sprite, 12, 19, 8, 2, PALETTE.tealLight);
  line(sprite, 16, 21, 16, 31, PALETTE.ink);
  put(sprite, 14, 25, PALETTE.wallLight);
  put(sprite, 18, 25, PALETTE.wallLight);

  // Two balanced legs and shoes, no walk pose.
  fillRect(sprite, 10, 31, 6, 6, PALETTE.ink);
  fillRect(sprite, 17, 31, 6, 6, PALETTE.ink);
  fillRect(sprite, 11, 31, 4, 4, PALETTE.wall);
  fillRect(sprite, 18, 31, 4, 4, PALETTE.wall);
  fillRect(sprite, 10, 36, 6, 2, PALETTE.ink);
  fillRect(sprite, 17, 36, 6, 2, PALETTE.ink);

  return { width, height, layers: [sprite] };
}

function crc32(buffer) {
  let crc = 0xffffffff;
  for (const byte of buffer) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit += 1) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const typeBuffer = Buffer.from(type, "ascii");
  const output = Buffer.alloc(data.length + 12);
  output.writeUInt32BE(data.length, 0);
  typeBuffer.copy(output, 4);
  data.copy(output, 8);
  output.writeUInt32BE(crc32(Buffer.concat([typeBuffer, data])), data.length + 8);
  return output;
}

function png(img) {
  const scanlines = Buffer.alloc((img.width * 4 + 1) * img.height);
  for (let y = 0; y < img.height; y += 1) {
    const destination = y * (img.width * 4 + 1);
    scanlines[destination] = 0;
    img.data.copy(scanlines, destination + 1, y * img.width * 4, (y + 1) * img.width * 4);
  }
  const header = Buffer.alloc(13);
  header.writeUInt32BE(img.width, 0);
  header.writeUInt32BE(img.height, 4);
  header[8] = 8;
  header[9] = 6;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk("IHDR", header),
    chunk("IDAT", zlib.deflateSync(scanlines, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

function piskelLayer(img, name) {
  return JSON.stringify({
    name,
    opacity: 1,
    frameCount: 1,
    chunks: [{ layout: [[0]], base64PNG: `data:image/png;base64,${png(img).toString("base64")}` }],
  });
}

function writePiskel(fileName, projectName, artwork, layerNames) {
  const source = {
    modelVersion: 2,
    piskel: {
      name: projectName,
      description: "Hand-authored fixed-palette pixel art for Do Not Open",
      fps: 12,
      height: artwork.height,
      width: artwork.width,
      layers: artwork.layers.map((layer, index) => piskelLayer(layer, layerNames[index])),
    },
  };
  fs.writeFileSync(path.join(sourceDir, fileName), `${JSON.stringify(source, null, 2)}\n`);
}

writePiskel(
  "apartment-map.piskel",
  "Do Not Open - Apartment",
  buildApartmentLayers(),
  ["Background", "Floors", "Walls", "Furniture", "Atmosphere"],
);
writePiskel(
  "player-idle.piskel",
  "Do Not Open - Player Idle",
  buildPlayerLayers(),
  ["Player"],
);

console.log(`Pixelorama sources written to ${sourceDir}`);
