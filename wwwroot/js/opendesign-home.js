/* SQL field and Open Game highlights; tournaments retain their separate prototype store. */
(() => {
  'use strict';
  const store=window.KicksStore;
  const esc=value=>store.escape(String(value??''));
  let sqlFields=[],sqlTotal=0,sqlGames=[],gamesError=false;
  const fieldsGrid=document.getElementById('home-fields-grid');
  const gamesGrid=document.getElementById('home-open-games-grid');
  const cupsGrid=document.getElementById('home-tournaments-grid');
  const form=document.getElementById('homeSearch');
  const city=document.getElementById('homeLocation'),date=document.getElementById('homeDate'),time=document.getElementById('homeTime');
  date.disabled=true;time.disabled=true;date.title=time.title='Scheduling will be connected in a later phase';
  const status=document.getElementById('homeSearchStatus'),resultBar=document.getElementById('homeSearchResults');
  const reduced=matchMedia('(prefers-reduced-motion: reduce)');
  let filters=null,lenis;
  function ammanNow(){
    const parts=new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Amman',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',hourCycle:'h23'}).formatToParts(new Date());
    const p=Object.fromEntries(parts.map(part=>[part.type,part.value]));
    return {date:`${p.year}-${p.month}-${p.day}`,hour:Number(p.hour)};
  }
  const arrow='<svg width="15" height="15" viewBox="0 0 20 20" fill="none" aria-hidden="true"><path d="M3.5 10h12M10.5 4.5 16 10l-5.5 5.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg>';
  function fieldCard(field,index){
    const image=window.KicksAuth.fieldImage(field),lead=index===0;
    return `<a class="venue ${lead?'venue--lead':'venue--sm'} fl-field-card pitch-editorial ${lead?'pitch-editorial--featured':'pitch-editorial--secondary'}" href="Pages/Fields/field-details.html?id=${encodeURIComponent(field.id)}"><div class="venue-media"><img src="${esc(image)}" alt="Football pitch at ${esc(field.name)}" width="1200" height="842" loading="lazy"><span class="venue-tag">${esc(field.type)}</span></div><div class="pitch-copy"><div class="pitch-identity"><p class="venue-meta">${esc([field.city,field.area].filter(Boolean).join(' / '))}</p><h3>${esc(field.name)}</h3><p class="venue-desc">${esc(field.description||field.amenities||field.type)}</p></div><div class="pitch-foot"><p class="venue-price"><strong>${esc(field.price)} JD</strong><span>/ hour</span></p><span class="venue-link">View pitch ${arrow}</span></div></div></a>`;
  }
  function emptyFields(){return `<div class="empty-fields-copy"><p class="kicker">Your next game is out there</p><h3>${filters?'Try another search.':'Great games start here.'}</h3><p>${filters?'No fields match this search. Try another location, date, or time.':'No fields published yet. Explore KICKS, or register as a field owner to bring your pitch to the community.'}</p><a class="text-link" href="${filters?'Pages/Fields/fields.html':'Pages/Auth/register.html'}">${filters?'Explore all fields':'List your field'} ${arrow}</a></div>`;}
  function gameCard(game){
    const start=new Date(game.startsAt),day=start.toLocaleDateString('en-GB',{weekday:'short',day:'numeric',month:'short',timeZone:'Asia/Amman'}),hour=start.toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit',timeZone:'Asia/Amman'}),spots=Math.max(0,game.capacity-game.count);
    return `<a class="game-row fl-game-row" href="Pages/OpenGames/game-details.html?id=${encodeURIComponent(game.id)}"><div class="game-date"><span>${esc(day)}</span><strong>${esc(hour)}</strong></div><div class="game-info"><h3>${esc(game.title)}</h3><p>${esc(game.field)} · ${esc(game.city)}</p><p>${esc(game.format)} · ${game.durationMinutes} min · No payment collected</p></div><span class="game-spots">${spots?spots+' spots left':'Full'}</span><span class="game-arrow">${arrow}</span></a>`;
  }
  function cupCard(cup,fields){
    const field=fields.find(f=>f.id===cup.fieldId);if(!field)return '';
    return `<a class="fl-cup-row" href="Pages/Tournaments/tournament-details.html?id=${encodeURIComponent(cup.id)}"><div><p>${esc(cup.date)} / ${esc(field.city)}</p><strong>${esc(cup.name)}</strong><p>${esc(cup.teams)} / ${esc(cup.capacity)} teams · ${esc(cup.fee)} JD / team</p></div>${arrow}</a>`;
  }
  function renderHighlights(){
    const fields=sqlFields,selected=city.value;
    const cities=[...new Set([...city.options].map(o=>o.value).filter(Boolean).concat(fields.map(f=>f.city)))].sort();
    if(selected&&!cities.includes(selected))cities.push(selected);
    if(city.dataset.cities!==cities.join('|')){city.dataset.cities=cities.join('|');city.replaceChildren(new Option('All locations',''),...cities.map(c=>new Option(c,c)));city.value=selected;}
    date.min=ammanNow().date;
    const matches=fields.filter(f=>!filters||!filters.city||f.city===filters.city);
    const cards=matches.slice(0,filters?12:3);fieldsGrid.innerHTML=cards.length?cards.map(fieldCard).join(''):emptyFields();
    if(filters){resultBar.hidden=false;const description=[filters.city,filters.date,filters.time].filter(Boolean).join(' · ');status.textContent=`${sqlTotal} ${sqlTotal===1?'field matches':'fields match'}${description?' · '+description:''}.${sqlTotal>12?' Showing the first 12.':''} Explore the field details.`;}
    const byStart=(a,b)=>(a.date+a.time).localeCompare(b.date+b.time);
    gamesGrid.innerHTML=gamesError?'<p class="home-empty">Game information is temporarily unavailable.</p>':sqlGames.filter(g=>Date.parse(g.startsAt)>Date.now()).slice(0,3).map(gameCard).join('')||'<p class="home-empty">No upcoming games yet. Check back for your next kick-off.</p>';
    cupsGrid.innerHTML=store.publicActivities('tournaments').sort(byStart).slice(0,1).map(c=>cupCard(c,store.list('fields'))).join('')||'<p class="home-empty">No active or upcoming tournaments yet. Get your team ready for the next one.</p>';
    document.getElementById('homeCities').textContent=cities.length?cities.join(' · '):'Jordan / Everyone’s game';
    window.ScrollTrigger?.refresh();
    const settings=store.settings(),aboutTitle=document.getElementById('homeAboutTitle'),aboutDescription=document.getElementById('homeAboutDescription'),contact=document.getElementById('homeContact');
    if(aboutTitle&&settings.title?.trim())aboutTitle.textContent=settings.title;
    if(aboutDescription&&settings.description?.trim())aboutDescription.textContent=settings.description;
    if(contact&&settings.contact){contact.textContent=settings.contact;contact.hidden=false;}
    document.dispatchEvent(new CustomEvent('kicks:home-rendered'));
  }
  if(store){
    for(let hour=10;hour<=22;hour++)time.add(new Option(`${String(hour).padStart(2,'0')}:00`,`${String(hour).padStart(2,'0')}:00`));
    time.addEventListener('change',()=>{date.required=Boolean(time.value);});
    form.addEventListener('submit',async event=>{
      event.preventDefault();date.min=ammanNow().date;if(!form.reportValidity())return;
      filters={city:city.value,date:'',time:''};try{const response=await fetch('/api/fields?City='+encodeURIComponent(city.value),{cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();sqlTotal=data.total;sqlFields=data.items.map(f=>({...f,amenities:f.amenities.map(a=>a.name).join(', ')}));sqlTotal=data.total;renderHighlights();}catch{status.textContent='Field information is temporarily unavailable.';return;}
      document.getElementById('fields').scrollIntoView({behavior:reduced.matches?'instant':'smooth',block:'start'});
      status.tabIndex=-1;status.focus({preventScroll:true});
    });
    document.getElementById('homeSearchReset').addEventListener('click',async()=>{
      filters=null;form.reset();date.required=false;resultBar.hidden=true;try{const response=await fetch('/api/fields',{cache:'no-store'});if(!response.ok)throw Error();const data=await response.json();sqlTotal=data.total;sqlFields=data.items.map(f=>({...f,amenities:f.amenities.map(a=>a.name).join(', ')}));renderHighlights();}catch{status.textContent='Field information is temporarily unavailable.';}
      const heading=document.getElementById('fieldsTitle');heading.tabIndex=-1;heading.focus({preventScroll:true});
    });
    renderHighlights();store.watchActivityClock(renderHighlights);
    addEventListener('storage',event=>{if(event.key==='kicks-data-v2')renderHighlights();});
    addEventListener('kicks-data-change',renderHighlights);
  }


fetch('/api/games',{cache:'no-store'}).then(r=>{if(!r.ok)throw Error();return r.json();}).then(data=>{sqlGames=data.items;renderHighlights();}).catch(()=>{gamesError=true;renderHighlights();});
fetch('/api/fields?Sort=newest',{cache:'no-store'}).then(r=>{if(!r.ok)throw Error();return r.json();}).then(data=>{sqlTotal=data.total;sqlFields=data.items.map(f=>({...f,amenities:f.amenities.map(a=>a.name).join(', ')}));renderHighlights();}).catch(()=>{fieldsGrid.innerHTML='<p class="home-empty">Field information is temporarily unavailable.</p>';});
fetch('/api/field-cities',{cache:'no-store'}).then(r=>r.json()).then(cities=>{for(const name of cities)if(![...city.options].some(o=>o.value===name))city.add(new Option(name,name));}).catch(()=>{});
})();
