// بحث الملاعب وفلترتها وتقسيم النتائج إلى صفحات. نقرأ البيانات مباشرة من التخزين.
let fields = []; let total = 0; let requestVersion = 0;
const filterForm = document.querySelector("#filterForm");
const search = document.querySelector("#fieldSearch");
const sort = document.querySelector("#sortFields");
const grid = document.querySelector("#fieldGrid");
const pagination = document.querySelector("#pagination");

let applied = {};
let page = 1;
const pageSize = 12;

function getResults() { return fields; }
async function render() {
  const version = ++requestVersion;
  const parameters = new URLSearchParams({Search:search.value.trim(),Sort:sort.value === 'recommended' ? 'newest' : sort.value,Page:page});
  for (const [name,key] of [['location','City'],['type','Format'],['min','Min'],['max','Max'],['rating','Rating']]) if(applied[name]) parameters.set(key,applied[name]);
  try {
    const response=await fetch('/api/fields?'+parameters,{cache:'no-store'});
    if(!response.ok) throw Error('Check your search and price range, then try again.');
    const result=await response.json(); if(version!==requestVersion)return;
    fields=result.items.map(f=>({...f,amenities:f.amenities.map(a=>a.name).join(', ')}));total=result.total;page=result.page;
  } catch(error) { if(version!==requestVersion)return;grid.replaceChildren();document.querySelector('#resultsCount').textContent=error.message;return; }

  const results = getResults();
  const pages = Math.ceil(total / pageSize);
  page = Math.min(page, pages || 1);
  document.querySelector("#resultsCount").textContent =
    `${total} fields found`;
  document.querySelector("#resultsRange").textContent = results.length
    ? `Showing ${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, total)} of ${total} fields`
    : "Try a different search or clear your filters.";
  let fieldCards = "";
  const firstField = (page - 1) * pageSize;
  const lastField = page * pageSize;
  const visibleFields = results;
  for (const field of visibleFields) {
    let availability = "Published field";
    let availabilityClass = "later";
    if (field.now) {
      availability = "Available now";
      availabilityClass = "";
    } else if (field.today) {
      availability = "Available today";
    }
    const presentationAmenities = String(field.amenities || '').split(/[,\n]/).map(item => item.trim()).filter(Boolean).slice(0, 2);
    const featured = visibleFields.indexOf(field) === 0;
    const fieldHref = 'field-details.html?id=' + encodeURIComponent(field.id);
    fieldCards += `
      <article class="kx-venue catalog-card${featured ? ' is-featured' : ''}">
        <a class="kx-venue-image catalog-image" href="${fieldHref}" aria-label="Explore ${KicksStore.escape(field.name)}">
          <img src="${KicksAuth.fieldImage(field)}" alt="Football pitch at ${KicksStore.escape(field.name)}" loading="${featured ? 'eager' : 'lazy'}" decoding="async">
          <span class="kx-venue-format">${KicksStore.escape(field.type)}</span>
          <span class="kx-venue-image-caption">${featured ? 'THE GROUND / IN FOCUS' : 'EXPLORE THE VENUE'} <span aria-hidden="true">&#8599;</span></span>
        </a>
        <div class="kx-venue-body catalog-body">
          <div class="kx-venue-topline"><span class="kx-badge ${availabilityClass ? 'kx-badge--muted' : 'kx-badge--positive'}"><i class="kx-badge-dot" aria-hidden="true"></i>${availability}</span>${field.rating != null && Number.isFinite(Number(field.rating)) ? '<span class="kx-venue-rating"><span aria-hidden="true">&#9733;</span> ' + Number(field.rating).toFixed(1) + '</span>' : ''}</div>
          <div class="kx-venue-title"><p class="kx-venue-city">${KicksStore.escape(field.city)}</p><h3><a href="${fieldHref}">${KicksStore.escape(field.name)}</a></h3><p class="kx-venue-location catalog-location">${KicksStore.escape(field.area)}, ${KicksStore.escape(field.city)}</p>${window.KicksNearby?.badge("fields",field.id) || ''}</div>
          ${featured && field.description ? '<p class="kx-venue-intro">' + KicksStore.escape(field.description) + '</p>' : ''}
          <div class="kx-venue-amenities amenities">${presentationAmenities.map(item => '<span>' + KicksStore.escape(item) + '</span>').join('') || '<span>Facilities not listed</span>'}</div>
          <footer class="kx-venue-footer"><p><strong>${field.price}</strong><span><b>JOD</b> / hour</span></p><a class="kx-btn kx-btn--ink view-field" href="${fieldHref}" aria-label="Explore venue: ${KicksStore.escape(field.name)}">Explore venue <span aria-hidden="true">&#8599;</span></a></footer>
        </div>
      </article>`;
  }
  grid.innerHTML = fieldCards;
  document.querySelector("#emptyState").hidden = results.length > 0;
  // نبني أزرار الصفحات بالتسلسل بدل تعبير واحد طويل.
  let pageButtons = "";
  if (pages > 1) {
    let previousDisabled = "";
    if (page === 1) previousDisabled = "disabled";
    pageButtons +=
      '<button data-page="' +
      (page - 1) +
      '" ' +
      previousDisabled +
      ' aria-label="Previous page">←</button>';
    for (let pageNumber = Math.max(1,page-2); pageNumber <= Math.min(pages,page+2); pageNumber++) {
      let currentPage = "";
      if (page === pageNumber) currentPage = 'aria-current="page"';
      pageButtons +=
        '<button data-page="' +
        pageNumber +
        '" ' +
        currentPage +
        ">" +
        pageNumber +
        "</button>";
    }
    let nextDisabled = "";
    if (page === pages) nextDisabled = "disabled";
    pageButtons +=
      '<button data-page="' +
      (page + 1) +
      '" ' +
      nextDisabled +
      ' aria-label="Next page">→</button>';
  }
  pagination.innerHTML = pageButtons;

  let hasFilters = false;
  for (const value of Object.values(applied)) {
    if (value) hasFilters = true;
  }
  const filterMessage = document.querySelector("#activeFilters");
  filterMessage.textContent = "All pitches. All possibilities.";
  if (hasFilters)
    filterMessage.textContent = "Filters applied · Clear to see all fields";
}

filterForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const values = {};
  for (const [name, value] of new FormData(filterForm)) {
    values[name] = value;
  }
  const max = filterForm.elements.max;
  max.setCustomValidity("");
  if (values.min && values.max && Number(values.min) > Number(values.max)) {
    max.setCustomValidity("Maximum price must be at least the minimum price.");
  }
  if (!filterForm.reportValidity()) return;
  applied = values;
  page = 1;
  render();
});
filterForm.addEventListener("input", () =>
  filterForm.elements.max.setCustomValidity(""),
);
function clearFilters() {
  window.KicksNearby?.clear(false);
  filterForm.reset();
  filterForm.elements.max.setCustomValidity("");
  applied = {};
  search.value = "";
  page = 1;
  render();
}
document
  .querySelectorAll("[data-clear]")
  .forEach((button) => button.addEventListener("click", clearFilters));
search.addEventListener("input", () => {
  page = 1;
  render();
});
document.querySelector("#searchForm").addEventListener("submit", (event) => {
  event.preventDefault();
  page = 1;
  render();
  document
    .querySelector("#results")
    .scrollIntoView({ behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth", block: "start" });
});
sort.addEventListener("change", () => {
  page = 1;
  render();
});
pagination.addEventListener("click", (event) => {
  const button = event.target.closest("[data-page]");
  if (!button || button.disabled) return;
  page = Number(button.dataset.page);
  render();
  document
    .querySelector("#results")
    .scrollIntoView({ behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth", block: "start" });
});
const toggle = document.querySelector("#filterToggle");
toggle.addEventListener("click", () => {
  const open = toggle.getAttribute("aria-expanded") !== "true";
  toggle.setAttribute("aria-expanded", String(open));
  document.querySelector("#filterPanel").classList.toggle("is-open", open);
});
render();

window.addEventListener("kicks-nearby-change", () => { page = 1; render(); });

fetch('/api/field-cities',{cache:'no-store'}).then(r=>r.json()).then(cities=>{const select=document.getElementById('location');for(const city of cities)if(![...select.options].some(o=>o.value===city))select.add(new Option(city,city));}).catch(()=>{});
