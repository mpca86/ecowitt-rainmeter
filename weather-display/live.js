import {extremes,dayKey,ZONE} from './model.mjs';
const $=id=>document.getElementById(id);
const fmt=(v,unit='')=>Number.isFinite(v)?v.toLocaleString('sk-SK',{maximumFractionDigits:1,minimumFractionDigits:1})+unit:'—';
const time=t=>new Intl.DateTimeFormat('sk-SK',{timeZone:ZONE,hour:'2-digit',minute:'2-digit'}).format(t);
export function startLive(){
  document.title='Ecowitt Weather Display · LIVE';
  document.querySelector('.status b').textContent='ECOWITT · WEB API';
  document.querySelector('footer span').textContent='Meranie Ecowitt · cloud';
  $('scenario').hidden=true;$('outage').hidden=true;
  $('scenario').onchange=null;$('outage').onclick=null;
  let state=null,connectionFailed=false;
  for(const id of ['temp','feelslike','wind','gust','maxgust','rain','rate','hourrain','humidity','pressure','uv','solar','min','max','mintime','maxtime'])$(id).textContent='—';
  $('trend').textContent='';$('direction').textContent='';$('rainlabel').textContent='';
  function render(){
    const now=Date.now(),v=state?.latest;
    $('clock').textContent=time(now);
    $('date').textContent=new Intl.DateTimeFormat('sk-SK',{timeZone:ZONE,weekday:'long',day:'numeric',month:'long'}).format(now);
    const age=v?Math.max(0,(now-v.ts)/1000):Infinity;
    const stale=age>180||connectionFailed||!!state?.error;
    document.body.classList.toggle('stale',stale);
    $('status').textContent=!v?'Čakám na prvé cloudové meranie':stale?'Neaktuálne údaje · posledné platné meranie':'Údaje aktuálne';
    $('updated').textContent=v?`Čas merania ${time(v.ts)} · pred ${Math.floor(age/60)} min`:'Zatiaľ bez merania';
    if(!v)return;
    $('station').textContent=state.station;
    for(const [id,unit]of Object.entries({temp:'',feelslike:' °C',wind:'',gust:' km/h',rain:'',rate:' mm/h',hourrain:' mm',humidity:' %',pressure:' hPa',uv:'',solar:' W/m²'}))$(id).textContent=fmt(v[id],unit);
    const directions=['S','SV','V','JV','J','JZ','Z','SZ'];
    $('direction').textContent=Number.isFinite(v.direction)?`${directions[Math.round(v.direction/45)%8]} · ${Math.round(v.direction)}°`:'—';
    const points=state.history||[],e=extremes(points,now);
    $('min').textContent=e?fmt(e.min.temp,'°'):'—';$('max').textContent=e?fmt(e.max.temp,'°'):'—';
    $('mintime').textContent=e?time(e.min.ts):'bez merania';$('maxtime').textContent=e?time(e.max.ts):'bez merania';
    document.querySelectorAll('.extremes label').forEach((label,i)=>label.textContent=i?'MAX DNES · ZÁZNAM':'MIN DNES · ZÁZNAM');
    const today=dayKey(now);
    const gusts=points.filter(p=>dayKey(p.ts)===today&&Number.isFinite(p.gust)).map(p=>p.gust);
    $('maxgust').textContent=gusts.length?fmt(Math.max(...gusts),' km/h'):'—';
    document.querySelector('#maxgust').previousElementSibling.textContent='Max náraz · zo záznamu';
    $('rainlabel').textContent=Number.isFinite(v.rate)?v.rate>0?'Práve prší':'Bez aktuálnych zrážok':'—';
    $('trend').textContent='';
    graph(points,now);
  }
  function graph(points,now){
    $('chart').replaceChildren();$('range').textContent='História od spustenia zberu';
    if(!points.length)return;
    const lo=Math.floor(Math.min(...points.map(p=>p.temp))-1),hi=Math.ceil(Math.max(...points.map(p=>p.temp))+1);
    const start=now-48*3600000,x=t=>55+(t-start)/(48*3600000)*1715,y=v=>175-(v-lo)/(hi-lo)*145;
    const ns='http://www.w3.org/2000/svg';
    function add(tag,attrs,label){const el=document.createElementNS(ns,tag);for(const[k,v]of Object.entries(attrs))el.setAttribute(k,v);if(label)el.textContent=label;$('chart').append(el)}
    for(let i=0;i<4;i++){const v=lo+(hi-lo)*i/3;add('line',{x1:55,x2:1770,y1:y(v),y2:y(v),stroke:'#2a3b50'});add('text',{x:0,y:y(v)+5,fill:'#a8b9cc','font-size':19},`${Math.round(v)}°`)}
    let path='';points.forEach((p,i)=>{path+=`${!i||p.ts-points[i-1].ts>180000?'M':'L'}${x(p.ts)},${y(p.temp)} `});
    add('path',{d:path,fill:'none',stroke:'#80dfe5','stroke-width':4});
    const last=points.at(-1);add('circle',{cx:x(last.ts),cy:y(last.temp),r:4,fill:'#80dfe5'});
    for(let i=0;i<5;i++){const t=start+i*12*3600000;add('text',{x:x(t),y:205,'text-anchor':i===0?'start':i===4?'end':'middle',fill:'#a8b9cc','font-size':19},new Intl.DateTimeFormat('sk-SK',{timeZone:ZONE,day:'numeric',month:'numeric',hour:'2-digit',minute:'2-digit'}).format(t))}
  }
  async function refresh(){try{const r=await fetch('/api/weather',{cache:'no-store',signal:AbortSignal.timeout(10000)});if(!r.ok)throw Error();const next=await r.json();if(next.mode!=='live')throw Error();state=next;connectionFailed=false}catch{connectionFailed=true}render()}
  refresh();setInterval(refresh,15000);setInterval(render,1000);
}
