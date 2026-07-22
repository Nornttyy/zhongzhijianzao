#!/usr/bin/env node

// Creates beginner-friendly layered Piskel files that Pixelorama can open.
// The guide layer is intentionally separate so it can be hidden before export.

const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const workspace = path.resolve(__dirname, "../..");
const templateDir = path.join(workspace, "Art", "StarterKit", "Templates");
fs.mkdirSync(templateDir, { recursive: true });

const TRANSPARENT = [0, 0, 0, 0];
const GRID = [111, 149, 148, 190];
const MAJOR = [213, 108, 92, 220];
const CENTRE = [236, 200, 117, 180];

function image(width, height, fill = TRANSPARENT) {
  const data = Buffer.alloc(width * height * 4);
  for (let index = 0; index < width * height; index += 1) data.set(fill, index * 4);
  return { width, height, data };
}

function put(img, x, y, color) {
  if (x < 0 || y < 0 || x >= img.width || y >= img.height) return;
  img.data.set(color, (y * img.width + x) * 4);
}

function horizontal(img, y, color) {
  for (let x = 0; x < img.width; x += 1) put(img, x, y, color);
}

function vertical(img, x, color) {
  for (let y = 0; y < img.height; y += 1) put(img, x, y, color);
}

function cellGrid(width, height, cellWidth, cellHeight) {
  const guide = image(width, height);
  for (let x = 0; x < width; x += cellWidth) vertical(guide, x, GRID);
  for (let y = 0; y < height; y += cellHeight) horizontal(guide, y, GRID);
  vertical(guide, width - 1, GRID);
  horizontal(guide, height - 1, GRID);
  return guide;
}

function playerGuide() {
  const guide = image(32, 40);
  vertical(guide, 0, GRID);
  vertical(guide, 31, GRID);
  horizontal(guide, 0, GRID);
  horizontal(guide, 39, GRID);
  vertical(guide, 15, CENTRE);
  vertical(guide, 16, CENTRE);
  horizontal(guide, 16, MAJOR);
  horizontal(guide, 36, MAJOR);
  return guide;
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
  const stride = img.width * 4 + 1;
  const scanlines = Buffer.alloc(stride * img.height);
  for (let y = 0; y < img.height; y += 1) {
    const destination = y * stride;
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

function layer(img, name) {
  return JSON.stringify({
    name,
    opacity: 1,
    frameCount: 1,
    chunks: [{ layout: [[0]], base64PNG: `data:image/png;base64,${png(img).toString("base64")}` }],
  });
}

function writeTemplate(fileName, projectName, width, height, guide) {
  const artwork = image(width, height);
  const project = {
    modelVersion: 2,
    piskel: {
      name: projectName,
      description: "Pixelorama beginner template — hide the guide layer before exporting",
      fps: 1,
      height,
      width,
      layers: [layer(artwork, "你的绘画"), layer(guide, "参考线（导出前隐藏）")],
    },
  };
  fs.writeFileSync(path.join(templateDir, fileName), `${JSON.stringify(project, null, 2)}\n`);
}

writeTemplate("player-single-frame.piskel", "Player Single Frame", 32, 40, playerGuide());
writeTemplate("world-tiles-32.piskel", "World Tiles 32", 256, 128, cellGrid(256, 128, 32, 32));
writeTemplate("crops-32.piskel", "Crop Growth 32", 256, 96, cellGrid(256, 96, 32, 32));
writeTemplate("items-16.piskel", "Inventory Items 16", 256, 128, cellGrid(256, 128, 16, 16));
writeTemplate("building-parts-32.piskel", "Building Parts 32", 256, 128, cellGrid(256, 128, 32, 32));

console.log(`Drawing templates written to ${templateDir}`);
