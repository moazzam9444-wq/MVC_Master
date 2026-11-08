(async () => {
 const select=id=>document.getElementById(id);
 try {
  const id=new URLSearchParams(location.search).get('id'); if(!/^\d+$/.test(id||''))throw Error('Not found');
  const response=await fetch('/api/fields/'+id,{cache:'no-store'});if(!response.ok)throw Error('Not found');
  const raw=await response.json();const field={...raw,amenities:raw.amenities.map(a=>a.name).join(', ')};
  select("fieldContent").hidden = false;
  document.title = `${field.name} | KICKS`;
  const place = `${field.area}, ${field.city}`;
  for (const id of ["fieldName", "breadcrumbName"])
    select(id).textContent = field.name;
  for (const id of ["fieldLocation", "mapLocation"])
    select(id).textContent = place;
  select("fieldRating").textContent = Number(field.rating || 0).toFixed(1);
  select("fieldFormat").textContent = field.type;
  select("hourlyPrice").textContent = field.price;
  select("fieldDescription").textContent =
    field.description ||
    `${field.name} is a ${field.type} football pitch in ${place}. A space for friendly games, weekly team sessions and your next kickabout.`;
  const amenities = document.querySelector(".amenity-grid");
  amenities.replaceChildren();
  const features = (field.amenities || "")
    .split(/[,\n]/)
    .map((item) => item.trim())
    .filter(Boolean);
  for (const feature of features) {
    const label = document.createElement("span");
    label.textContent = feature;
    amenities.append(label);
  }
  if (!features.length)
    amenities.textContent = "No facilities listed by the venue yet.";
  for (const id of ["fieldPhoto", "largePhoto"]) {
    select(id).src = `${KicksAuth.fieldImage(field)}`;
    select(id).alt = `Football pitch at ${field.name}`;
  }
  select("mapLink").href =
    `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(place + ", Jordan")}`;

  for(const [selector,text] of [['[data-kx-location]',place],['[data-kx-format]',field.type]])document.querySelector(selector).textContent=text;
  document.querySelector('[data-kx-owner]').closest('dl>div').hidden=true;
  document.querySelectorAll('[data-kx-photo-crop]').forEach(image=>{image.src=field.photo;image.alt=field.name;});
  if(!field.images.length)document.querySelectorAll('.kx-photo-note,[data-kx-large-photo-note]').forEach(note=>note.textContent='Illustrative pitch image / no venue photograph uploaded.');
  document.querySelector('[data-kx-mobile-price]').textContent=field.price+' JOD';
  KicksBookings.mount(field);
  const open=()=>select('photoDialog').showModal();select('enlargePhoto').onclick=open;document.querySelectorAll('[data-kx-photo]').forEach(b=>b.onclick=open);
  document.querySelector('[data-kx-mobile-label]').textContent='Per hour';
 }catch(error){select('missingField').hidden=false;select('fieldContent').hidden=true;}
})();