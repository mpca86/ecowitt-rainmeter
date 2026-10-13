export const ZONE='Europe/Bratislava';
export function dayKey(t){return new Intl.DateTimeFormat('sv-SE',{timeZone:ZONE,year:'numeric',month:'2-digit',day:'2-digit'}).format(t)}
export function history(now,scenario='day'){const end=Math.floor(now/300000)*300000;return Array.from({length:577},(_,i)=>{const ts=end-(576-i)*300000;const hour=Number(new Intl.DateTimeFormat('en-GB',{timeZone:ZONE,hour:'numeric',minute:'numeric',hourCycle:'h23'}).format(ts).slice(0,2));return {ts,temp:Math.round((10+6*Math.sin((hour-8)*Math.PI/12)+.7*Math.sin(i*.13)+(scenario==='rain'?-2:0))*10)/10}})}
export function extremes(points,now){const today=points.filter(p=>dayKey(p.ts)===dayKey(now));if(!today.length)return null;return today.reduce((e,p)=>({min:p.temp<e.min.temp?p:e.min,max:p.temp>e.max.temp?p:e.max}),{min:today[0],max:today[0]})}
export function freshness(last,now){const age=Math.max(0,now-last);return age>180000?'offline':age>45000?'stale':'fresh'}
