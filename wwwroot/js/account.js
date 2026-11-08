// Identity cookie authentication. The server-rendered profile is presentation data only.
{
  const state = JSON.parse(document.getElementById("kicks-auth-state")?.textContent || '{"profile":null}');
  const profile = state.profile ? Object.freeze(state.profile) : null;
  const siteRoot = new URL("../", document.currentScript.src);
  const store = KicksStore;
  function url(path) {
    return new URL(path, siteRoot).href;
  }
  function current() { return profile; }
  // الصورة المرفوعة لها الأولوية. إذا لا توجد صورة نستخدم الصورة الجاهزة.
  function fieldImage(field) {
    if (field && /^\/media\/fields\/[a-f0-9]{32}\.(jpg|png|webp)$/.test(field.photo || '')) return field.photo;
    if (field && /^data:image\/(jpeg|png|webp);base64,/.test(field.photo || '')) {
      return field.photo;
    }
    let imageNumber = 1;
    if (field && [1, 2, 3, 4].includes(Number(field.image))) {
      imageNumber = field.image;
    }
    return url('assets/images/field' + imageNumber + '.jpg');
  }

  // كل دور له صفحة داشبورد مختلفة.
  function dashboard(account = current()) {
    if (!account) return url('Pages/Auth/login.html');
    return url('Pages/Dashboard/' + account.role + '.html');
  }
  function loginUrl() {
    const target = new URL("Pages/Auth/login.html", siteRoot);
    target.searchParams.set(
      "returnTo",
      location.pathname + location.search + location.hash,
    );
    return target.href;
  }
  function notify(message) {
    let notice = document.getElementById("accountNotice");
    if (!notice) {
      notice = document.createElement("div");
      notice.id = "accountNotice";
      notice.className = "account-notice";
      notice.setAttribute("role", "status");
      document.body.append(notice);
    }
    notice.textContent = message;
    notice.hidden = false;
    clearTimeout(notify.timer);
    notify.timer = setTimeout(() => (notice.hidden = true), 5000);
  }
  function requirePlayer() {
    const account = current();
    if (!account) {
      location.assign(loginUrl());
      return null;
    }
    if (account.role !== "player") {
      notify("Use a player account to book or join an event.");
      return null;
    }
    return account;
  }
  function requireRole(role) {
    const account = current();
    if (!account) {
      location.replace(loginUrl());
      return null;
    }
    if (account.role !== role) {
      location.replace(dashboard(account));
      return null;
    }
    return account;
  }
  function logout() {
    const form = document.createElement("form");
    form.method = "post";
    form.action = url("Pages/Auth/logout");
    const token = document.createElement("input");
    token.type = "hidden"; token.name = "__RequestVerificationToken"; token.value = state.token || "";
    form.append(token); document.body.append(form); form.submit();
  }
  function serverOnly() { throw Error("Use the server account forms to manage authentication."); }
  function updateNavigation() {
    const nav = document.querySelector("#mainNavbar");
    if (!nav) return;
    nav
      .querySelectorAll('a[href*="Auth/login.html"]')
      .forEach((link) => link.remove());
    let slot = document.getElementById("navbarAccount");
    if (!slot) {
      slot = document.createElement("div");
      slot.id = "navbarAccount";
      slot.className = "navbar-account";
      nav.append(slot);
    }
    const account = current();
    if (!account) {
      slot.innerHTML =
        '<a class="account-login" href="' +
        url("Pages/Auth/login.html") +
        '">Log in ↗</a>';
      return;
    }
    const profileUrl = dashboard(account) + "#profile";
    const avatar = account.avatar || url("assets/images/avatar.svg");
    slot.innerHTML = `<button type="button" class="avatar-button" id="avatarMenuButton" aria-expanded="false" aria-controls="avatarMenu" aria-label="Open account menu"><img src="${store.escape(avatar)}" alt="${store.escape(account.name)}"><span>${store.escape(account.name.split(" ")[0])}</span><span aria-hidden="true">⌄</span></button><div class="avatar-menu" id="avatarMenu" hidden><strong>${store.escape(account.name)}</strong><small>${store.escape(account.email)}</small><a href="${profileUrl}">My profile</a><a href="${dashboard(account)}">Dashboard</a><button type="button" id="logoutAccount">Log out ↗</button></div>`;
    const button = document.getElementById("avatarMenuButton"),
      menu = document.getElementById("avatarMenu");
    button.addEventListener("click", () => {
      menu.hidden = !menu.hidden;
      button.setAttribute("aria-expanded", String(!menu.hidden));
    });
    document.getElementById("logoutAccount").addEventListener("click", logout);
  }
  document.addEventListener("click", (event) => {
    const menu = document.getElementById("avatarMenu");
    if (menu && !event.target.closest("#navbarAccount")) {
      menu.hidden = true;
      document
        .getElementById("avatarMenuButton")
        ?.setAttribute("aria-expanded", "false");
    }
  });
  // Let visitors browse, but send action buttons to sign-in before form validation.
  document.addEventListener('click', event => {
    if (!current() && event.target.closest('#bookButton, #teamSubmit, #previewJoin')) {
      event.preventDefault(); event.stopImmediatePropagation(); location.assign(loginUrl());
    }
  }, true);
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      const menu = document.getElementById("avatarMenu");
      if (menu && !menu.hidden) {
        menu.hidden = true;
        const button = document.getElementById("avatarMenuButton");
        button.setAttribute("aria-expanded", "false");
        button.focus();
      }
    }
  });
  window.KicksAuth = {
    current,
    url,
    dashboard,
    fieldImage,
    loginUrl,
    notify,
    requirePlayer,
    requireRole,
    register: serverOnly,
    validOwnershipProof: serverOnly,
    setAccountActive: serverOnly,
    login: serverOnly,
    afterLogin: serverOnly,
    passwordChanges: serverOnly,
    logout,
    refreshNavigation: updateNavigation,
  };
  window.addEventListener("kicks-account-change", updateNavigation);
  window.addEventListener("kicks-data-change", updateNavigation);
  window.addEventListener("pageshow", event => {
    if (event.persisted) location.reload();
    else updateNavigation();
  });
  updateNavigation();
}
