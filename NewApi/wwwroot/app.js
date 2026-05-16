const API = 'https://localhost:7023/api';

// ── State ──
let token = localStorage.getItem('token');
let username = localStorage.getItem('username');
let isAdmin = localStorage.getItem('isAdmin') === 'true';

// ── Views ──
const views = {
  auth: document.getElementById('view-auth'),
  posts: document.getElementById('view-posts'),
  create: document.getElementById('view-create'),
  admin: document.getElementById('view-admin'),
};

function showView(name) {
  Object.values(views).forEach(v => v.classList.add('hidden'));
  views[name].classList.remove('hidden');
}

function navigate(name) {
  if (name === 'posts') {
    loadPosts();
    document.getElementById('user-display-name').textContent = username;
    document.getElementById('btn-admin').classList.toggle('hidden', !isAdmin);
  }
  if (name === 'admin') {
    document.getElementById('user-search').value = '';
    loadUsers();
  }
  showView(name);
}

// ── Init ──
if (token) navigate('posts');
else showView('auth');

// ── Auth tabs ──
document.querySelectorAll('.tab').forEach(tab => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    tab.classList.add('active');
    const target = tab.dataset.tab;
    document.getElementById('form-login').classList.toggle('hidden', target !== 'login');
    document.getElementById('form-register').classList.toggle('hidden', target !== 'register');
  });
});

// ── Login ──
document.getElementById('form-login').addEventListener('submit', async e => {
  e.preventDefault();
  const email = document.getElementById('login-email').value;
  const password = document.getElementById('login-password').value;
  const err = document.getElementById('login-error');
  err.textContent = '';

  try {
    const res = await fetch(`${API}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) { err.textContent = 'Email ou mot de passe incorrect.'; return; }
    const data = await res.json();
    saveSession(data);
    navigate('posts');
  } catch {
    err.textContent = 'Erreur de connexion au serveur.';
  }
});

// ── Register ──
document.getElementById('form-register').addEventListener('submit', async e => {
  e.preventDefault();
  const username_r = document.getElementById('register-username').value;
  const email = document.getElementById('register-email').value;
  const password = document.getElementById('register-password').value;
  const err = document.getElementById('register-error');
  err.textContent = '';

  try {
    const res = await fetch(`${API}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: username_r, email, password }),
    });
    if (!res.ok) { err.textContent = 'Inscription échouée. Email déjà utilisé ?'; return; }

    const loginRes = await fetch(`${API}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    const data = await loginRes.json();
    saveSession(data);
    navigate('posts');
  } catch {
    err.textContent = 'Erreur de connexion au serveur.';
  }
});

function saveSession(data) {
  token = data.token;
  username = data.username;
  isAdmin = data.isAdmin;
  localStorage.setItem('token', token);
  localStorage.setItem('username', username);
  localStorage.setItem('isAdmin', isAdmin);
}

// ── User dropdown ──
const userTrigger = document.getElementById('user-trigger');
const userDropdown = document.getElementById('user-dropdown');

userTrigger.addEventListener('click', e => {
  e.stopPropagation();
  userDropdown.classList.toggle('hidden');
});

document.addEventListener('click', () => userDropdown.classList.add('hidden'));

document.getElementById('btn-logout').addEventListener('click', () => {
  token = null; username = null; isAdmin = false;
  localStorage.clear();
  showView('auth');
});

// ── Load posts ──
async function loadPosts() {
  const list = document.getElementById('posts-list');
  list.innerHTML = '<div class="loading">Chargement...</div>';

  try {
    const res = await fetch(`${API}/posts`);
    const posts = await res.json();

    if (!posts.length) {
      list.innerHTML = '<div class="loading">Aucun post pour l\'instant.</div>';
      return;
    }

    list.innerHTML = posts.map(p => `
      <div class="post-card">
        <h2>${escHtml(p.title)}</h2>
        <p>${escHtml(p.content)}</p>
        <div class="post-meta">
          <span class="author">${escHtml(p.authorUsername)}</span>
          <span>${formatDate(p.createdAt)}</span>
          <div class="reactions">
            <button class="reaction-btn" onclick="react(${p.id}, true)">👍 ${p.likes}</button>
            <button class="reaction-btn" onclick="react(${p.id}, false)">👎 ${p.dislikes}</button>
            ${isAdmin ? `<button class="delete-post-btn" onclick="deletePost(${p.id})">🗑 Supprimer</button>` : ''}
          </div>
        </div>
      </div>
    `).join('');
  } catch {
    list.innerHTML = '<div class="loading">Erreur lors du chargement.</div>';
  }
}

// ── React ──
async function react(postId, isLike) {
  if (!token) return;
  await fetch(`${API}/posts/${postId}/${isLike ? 'like' : 'dislike'}`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
  loadPosts();
}

// ── Delete post (admin) ──
async function deletePost(postId) {
  if (!confirm('Supprimer ce post ?')) return;
  await fetch(`${API}/posts/${postId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
  loadPosts();
}

// ── Navigate to create ──
document.getElementById('btn-create').addEventListener('click', () => showView('create'));
document.getElementById('btn-back').addEventListener('click', () => navigate('posts'));

// ── Create post ──
document.getElementById('form-create').addEventListener('submit', async e => {
  e.preventDefault();
  const title = document.getElementById('post-title').value;
  const content = document.getElementById('post-content').value;
  const err = document.getElementById('create-error');
  err.textContent = '';

  try {
    const res = await fetch(`${API}/posts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ title, content }),
    });
    if (!res.ok) { err.textContent = 'Erreur lors de la publication.'; return; }
    document.getElementById('post-title').value = '';
    document.getElementById('post-content').value = '';
    navigate('posts');
  } catch {
    err.textContent = 'Erreur de connexion au serveur.';
  }
});

// ── Admin navigation ──
document.getElementById('btn-admin').addEventListener('click', () => navigate('admin'));
document.getElementById('btn-admin-back').addEventListener('click', () => navigate('posts'));

// ── Load users ──
let allUsers = [];

async function loadUsers() {
  const list = document.getElementById('users-list');
  list.innerHTML = '<div class="loading">Chargement...</div>';

  try {
    const res = await fetch(`${API}/users`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    allUsers = await res.json();
    renderUsers(allUsers);
  } catch {
    list.innerHTML = '<div class="loading">Erreur lors du chargement.</div>';
  }
}

function renderUsers(users) {
  const list = document.getElementById('users-list');
  if (!users.length) {
    list.innerHTML = '<div class="loading">Aucun utilisateur trouvé.</div>';
    return;
  }
  list.innerHTML = users.map(u => `
    <div class="user-row">
      <img src="avatar.svg" class="avatar" alt="avatar" />
      <div class="user-row-info">
        <strong>${escHtml(u.username)} ${u.isAdmin ? '<span class="badge-admin">Admin</span>' : ''}</strong>
        <span>${escHtml(u.email)}</span>
      </div>
      <div class="user-row-actions">
        <button class="btn-info" onclick="showUserDetail(${u.id})">Infos</button>
        <button class="btn-delete-user" onclick="deleteUser(${u.id}, '${escHtml(u.username)}')">Supprimer</button>
      </div>
    </div>
  `).join('');
}

document.getElementById('user-search').addEventListener('input', e => {
  const q = e.target.value.toLowerCase();
  renderUsers(allUsers.filter(u => u.username.toLowerCase().includes(q)));
});

// ── User detail modal ──
async function showUserDetail(userId) {
  const overlay = document.getElementById('modal-overlay');
  const content = document.getElementById('modal-content');
  content.innerHTML = '<div class="loading">Chargement...</div>';
  overlay.classList.remove('hidden');

  try {
    const res = await fetch(`${API}/users/${userId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const u = await res.json();

    content.innerHTML = `
      <h3>${escHtml(u.username)} ${u.isAdmin ? '<span class="badge-admin">Admin</span>' : ''}</h3>
      <div class="modal-stat"><span>Email</span><span>${escHtml(u.email)}</span></div>
      <div class="modal-stat"><span>Membre depuis</span><span>${formatDate(u.createdAt)}</span></div>
      <div class="modal-stat"><span>Posts</span><span>${u.postCount}</span></div>
      <div class="modal-stat"><span>Commentaires</span><span>${u.commentCount}</span></div>
    `;
  } catch {
    content.innerHTML = '<p class="form-error">Erreur lors du chargement.</p>';
  }
}

document.getElementById('modal-close').addEventListener('click', () => {
  document.getElementById('modal-overlay').classList.add('hidden');
});

document.getElementById('modal-overlay').addEventListener('click', e => {
  if (e.target === e.currentTarget) e.currentTarget.classList.add('hidden');
});

// ── Delete user ──
async function deleteUser(userId, name) {
  if (!confirm(`Supprimer l'utilisateur "${name}" ?`)) return;
  await fetch(`${API}/users/${userId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
  loadUsers();
}

// ── Helpers ──
function escHtml(str) {
  return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
}
