const { setTimeout: delay } = require('node:timers/promises');

async function waitForPageCondition(page, predicate, description, timeoutMs = 5000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await page.evaluate(predicate)) return;
    await delay(40);
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

module.exports = { waitForPageCondition };
