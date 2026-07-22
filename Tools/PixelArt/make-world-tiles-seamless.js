#!/usr/bin/env node

// Removes the one-pixel frame around every 32 x 32 ground tile. Those frames
// become a distracting grid when Unity repeats a tile across the world.

const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const TILE_SIZE = 32;
const filePath = path.resolve(__dirname, "../../Assets/Resources/PixelArt/world-tiles.png");
const source = fs.readFileSync(filePath);
const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

if (!source.subarray(0, 8).equals(signature)) {
  throw new Error("world-tiles.png is not a PNG file");
}

let width;
let height;
let bitDepth;
let colorType;
let interlace;
const compressedParts = [];

for (let offset = 8; offset < source.length;) {
  const length = source.readUInt32BE(offset);
  const type = source.toString("ascii", offset + 4, offset + 8);
  const data = source.subarray(offset + 8, offset + 8 + length);

  if (type === "IHDR") {
    width = data.readUInt32BE(0);
    height = data.readUInt32BE(4);
    bitDepth = data[8];
    colorType = data[9];
    interlace = data[12];
  } else if (type === "IDAT") {
    compressedParts.push(data);
  } else if (type === "IEND") {
    break;
  }

  offset += length + 12;
}

if (bitDepth !== 8 || colorType !== 6 || interlace !== 0) {
  throw new Error("Expected a non-interlaced 8-bit RGBA PNG");
}
if (width % TILE_SIZE !== 0 || height % TILE_SIZE !== 0) {
  throw new Error("The atlas dimensions must be multiples of 32 pixels");
}

const bytesPerPixel = 4;
const stride = width * bytesPerPixel;
const filtered = zlib.inflateSync(Buffer.concat(compressedParts));
const pixels = Buffer.alloc(stride * height);
let filteredOffset = 0;
let previous = Buffer.alloc(stride);

function paeth(left, up, upperLeft) {
  const estimate = left + up - upperLeft;
  const leftDistance = Math.abs(estimate - left);
  const upDistance = Math.abs(estimate - up);
  const upperLeftDistance = Math.abs(estimate - upperLeft);
  if (leftDistance <= upDistance && leftDistance <= upperLeftDistance) return left;
  return upDistance <= upperLeftDistance ? up : upperLeft;
}

for (let y = 0; y < height; y += 1) {
  const filter = filtered[filteredOffset];
  filteredOffset += 1;
  const row = Buffer.alloc(stride);

  for (let x = 0; x < stride; x += 1) {
    const raw = filtered[filteredOffset + x];
    const left = x >= bytesPerPixel ? row[x - bytesPerPixel] : 0;
    const up = previous[x];
    const upperLeft = x >= bytesPerPixel ? previous[x - bytesPerPixel] : 0;
    let prediction = 0;

    if (filter === 1) prediction = left;
    else if (filter === 2) prediction = up;
    else if (filter === 3) prediction = Math.floor((left + up) / 2);
    else if (filter === 4) prediction = paeth(left, up, upperLeft);
    else if (filter !== 0) throw new Error(`Unsupported PNG filter: ${filter}`);

    row[x] = (raw + prediction) & 0xff;
  }

  row.copy(pixels, y * stride);
  previous = row;
  filteredOffset += stride;
}

function copyPixel(destinationX, destinationY, sourceX, sourceY) {
  const destination = (destinationY * width + destinationX) * bytesPerPixel;
  const from = (sourceY * width + sourceX) * bytesPerPixel;
  pixels.copy(pixels, destination, from, from + bytesPerPixel);
}

for (let tileY = 0; tileY < height; tileY += TILE_SIZE) {
  for (let tileX = 0; tileX < width; tileX += TILE_SIZE) {
    for (let y = tileY; y < tileY + TILE_SIZE; y += 1) {
      copyPixel(tileX, y, tileX + 1, y);
      copyPixel(tileX + TILE_SIZE - 1, y, tileX + TILE_SIZE - 2, y);
    }
    for (let x = tileX; x < tileX + TILE_SIZE; x += 1) {
      copyPixel(x, tileY, x, tileY + 1);
      copyPixel(x, tileY + TILE_SIZE - 1, x, tileY + TILE_SIZE - 2);
    }
  }
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

const scanlines = Buffer.alloc((stride + 1) * height);
for (let y = 0; y < height; y += 1) {
  const destination = y * (stride + 1);
  scanlines[destination] = 0;
  pixels.copy(scanlines, destination + 1, y * stride, (y + 1) * stride);
}

const header = Buffer.alloc(13);
header.writeUInt32BE(width, 0);
header.writeUInt32BE(height, 4);
header[8] = 8;
header[9] = 6;

const output = Buffer.concat([
  signature,
  chunk("IHDR", header),
  chunk("IDAT", zlib.deflateSync(scanlines, { level: 9 })),
  chunk("IEND", Buffer.alloc(0)),
]);

fs.writeFileSync(filePath, output);
console.log(`Removed tile frames from ${filePath}`);
