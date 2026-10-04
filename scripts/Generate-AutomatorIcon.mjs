import fs from 'node:fs';
import path from 'node:path';
import { deflateSync } from 'node:zlib';

const sparkShape = [[16, 5], [18, 13], [26, 15], [19, 17], [16, 27], [13, 18], [6, 16], [13, 14]];

function insidePolygon(x, y, points) {
  let inside = false;
  for (let current = 0, previous = points.length - 1; current < points.length; previous = current++) {
    const [cx, cy] = points[current];
    const [px, py] = points[previous];
    const crosses = (cy > y) !== (py > y) && x < ((px - cx) * (y - cy)) / (py - cy) + cx;
    if (crosses) inside = !inside;
  }
  return inside;
}

function crc32(buffer) {
  let crc = 0xffffffff;
  for (const byte of buffer) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function chunk(name, data) {
  const nameBuffer = Buffer.from(name, 'ascii');
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length);
  const checksum = Buffer.alloc(4);
  checksum.writeUInt32BE(crc32(Buffer.concat([nameBuffer, data])));
  return Buffer.concat([length, nameBuffer, data, checksum]);
}

function renderPng(size) {
  const scale = size / 32;
  const pixels = Buffer.alloc((size * 4 + 1) * size);
  const spark = sparkShape.map(([x, y]) => [x * scale, y * scale]);
  const inset = 8 * scale;
  const radius = 8 * scale;

  for (let y = 0; y < size; y++) {
    const rowOffset = y * (size * 4 + 1);
    pixels[rowOffset] = 0;
    for (let x = 0; x < size; x++) {
      const px = x + 0.5;
      const py = y + 0.5;
      const nearestX = Math.max(inset, Math.min(px, size - inset));
      const nearestY = Math.max(inset, Math.min(py, size - inset));
      const inRoundRect = (px - nearestX) ** 2 + (py - nearestY) ** 2 <= radius ** 2;
      const inSpark = insidePolygon(px, py, spark);
      const color = inSpark ? [255, 255, 255, 255] : inRoundRect ? [13, 116, 144, 255] : [0, 0, 0, 0];
      pixels.set(color, rowOffset + 1 + x * 4);
    }
  }

  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0);
  header.writeUInt32BE(size, 4);
  header[8] = 8;
  header[9] = 6;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk('IHDR', header),
    chunk('IDAT', deflateSync(pixels, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

const png = renderPng(32);
const icoPng = renderPng(256);

const icoHeader = Buffer.alloc(22);
icoHeader.writeUInt16LE(0, 0);
icoHeader.writeUInt16LE(1, 2);
icoHeader.writeUInt16LE(1, 4);
icoHeader.writeUInt16LE(1, 10);
icoHeader.writeUInt16LE(32, 12);
icoHeader.writeUInt32LE(icoPng.length, 14);
icoHeader.writeUInt32LE(22, 18);

const assetDirectory = path.resolve('assets');
fs.mkdirSync(assetDirectory, { recursive: true });
fs.writeFileSync(path.join(assetDirectory, 'automator.png'), png);
fs.writeFileSync(path.join(assetDirectory, 'automator.ico'), Buffer.concat([icoHeader, icoPng]));
process.stdout.write(`Generated assets/automator.png (${png.length} bytes, 32x32) and assets/automator.ico (${icoHeader.length + icoPng.length} bytes, 256x256)\n`);
