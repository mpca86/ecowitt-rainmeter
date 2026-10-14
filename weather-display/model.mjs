export const ZONE = 'Europe/Bratislava';
const calendar = new Intl.DateTimeFormat('sv-SE', {
  timeZone: ZONE, year: 'numeric', month: '2-digit', day: '2-digit',
});
const clock = new Intl.DateTimeFormat('en-GB', {
  timeZone: ZONE, hour: 'numeric', minute: 'numeric', hourCycle: 'h23',
});
export function dayKey(t) { return calendar.format(t); }
export function history(now, scenario = 'day') {
  const end = Math.floor(now / 300000) * 300000;
  return Array.from({ length: 577 }, (_, i) => {
    const ts = end - (576 - i) * 300000;
    const [hours, minutes] = clock.format(ts).split(':').map(Number);
    const hour = hours + minutes / 60;
    // Timestamp-based variation keeps overlapping history stable between refreshes.
    const variation = .7 * Math.sin(ts / 300000 * .13);
    return { ts, temp: Math.round((10 + 6 * Math.sin((hour - 8) * Math.PI / 12)
      + variation + (scenario === 'rain' ? -2 : 0)) * 10) / 10 };
  });
}
export function extremes(points, now) {
  const key = dayKey(now);
  const today = points.filter(p => Number.isFinite(p.ts) && Number.isFinite(p.temp)
    && p.ts <= now && dayKey(p.ts) === key).sort((a, b) => a.ts - b.ts);
  if (!today.length) return null;
  return today.reduce((e, p) => ({
    min: p.temp < e.min.temp ? p : e.min,
    max: p.temp > e.max.temp ? p : e.max,
  }), { min: today[0], max: today[0] });
}
export function freshness(last, now) {
  if (!Number.isFinite(last)) return 'offline';
  const age = Math.max(0, now - last);
  return age > 180000 ? 'offline' : age > 45000 ? 'stale' : 'fresh';
}

// Illustrative scenario offsets only; not an Ecowitt or meteorological formula.
export function demoFeelsLike(temperature, scenario = 'day') {
  if (!Number.isFinite(temperature)) return null;
  const offset = scenario === 'rain' ? -2.5 : scenario === 'night' ? -1.8 : -1.2;
  return Math.round((temperature + offset) * 10) / 10;
}
