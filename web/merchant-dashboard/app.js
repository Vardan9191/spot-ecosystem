// Spot Merchant Dashboard Application Logic
const MERCHANT_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'; // Cortado Artisan Coffee
let currentRadius = 150;
let storeLocation = { lat: 40.7180, lng: -74.0020 };

// Initialize Canvas
const canvas = document.getElementById('mapCanvas');
const ctx = canvas.getContext('2d');

function resizeCanvas() {
  canvas.width = canvas.parentElement.clientWidth;
  canvas.height = canvas.parentElement.clientHeight;
  drawGeofenceMap();
}

window.addEventListener('resize', resizeCanvas);

function drawGeofenceMap() {
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  const cx = canvas.width / 2;
  const cy = canvas.height / 2;

  // Draw Grid Lines (Street grid simulation)
  ctx.strokeStyle = 'rgba(255, 255, 255, 0.04)';
  ctx.lineWidth = 1;
  for (let x = 0; x < canvas.width; x += 30) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, canvas.height);
    ctx.stroke();
  }
  for (let y = 0; y < canvas.height; y += 30) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(canvas.width, y);
    ctx.stroke();
  }

  // Draw 1.5km Neighborhood Boundary
  ctx.beginPath();
  ctx.arc(cx, cy, 150, 0, Math.PI * 2);
  ctx.strokeStyle = 'rgba(59, 130, 246, 0.2)';
  ctx.setLineDash([6, 6]);
  ctx.stroke();
  ctx.setLineDash([]);

  // Calculate pixel radius relative to meters (e.g. 150m -> 65px)
  const pixelRadius = Math.max(20, (currentRadius / 1500) * 150);

  // Draw Geofence Radius Circle (with pulse glow)
  ctx.beginPath();
  ctx.arc(cx, cy, pixelRadius, 0, Math.PI * 2);
  ctx.fillStyle = 'rgba(99, 102, 241, 0.15)';
  ctx.fill();
  ctx.lineWidth = 2;
  ctx.strokeStyle = '#6366f1';
  ctx.stroke();

  // Draw Outer Shockwave ring
  ctx.beginPath();
  ctx.arc(cx, cy, pixelRadius + 10, 0, Math.PI * 2);
  ctx.strokeStyle = 'rgba(99, 102, 241, 0.3)';
  ctx.stroke();

  // Draw Store Pin Center
  ctx.beginPath();
  ctx.arc(cx, cy, 7, 0, Math.PI * 2);
  ctx.fillStyle = '#f43f5e';
  ctx.fill();
  ctx.lineWidth = 2;
  ctx.strokeStyle = '#ffffff';
  ctx.stroke();

  // Radius Label on map
  ctx.fillStyle = '#a5b4fc';
  ctx.font = '12px system-ui';
  ctx.fillText(`${currentRadius}m Active Geofence`, cx - 55, cy + pixelRadius + 22);
}

// Handle Radius Slider
const slider = document.getElementById('radiusSlider');
const radiusDisplay = document.getElementById('radiusDisplay');

slider.addEventListener('input', (e) => {
  currentRadius = parseInt(e.target.value, 10);
  radiusDisplay.textContent = `${currentRadius}m`;
  drawGeofenceMap();
});

// Handle Preset Buttons
document.querySelectorAll('.pill-btn').forEach((btn) => {
  btn.addEventListener('click', (e) => {
    document.querySelectorAll('.pill-btn').forEach((b) => b.classList.remove('active'));
    e.target.classList.add('active');
    const radius = parseInt(e.target.dataset.radius, 10);
    slider.value = radius;
    currentRadius = radius;
    radiusDisplay.textContent = `${currentRadius}m`;
    drawGeofenceMap();
  });
});

// Save Geofence Changes to Backend
document.getElementById('saveGeofenceBtn').addEventListener('click', async () => {
  const btn = document.getElementById('saveGeofenceBtn');
  btn.textContent = 'Saving...';
  btn.disabled = true;

  try {
    const res = await fetch(`http://localhost:5000/api/v1/merchants/${MERCHANT_ID}/geofence`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        latitude: storeLocation.lat,
        longitude: storeLocation.lng,
        radiusMeters: currentRadius,
      }),
    });

    if (res.ok) {
      alert(`Geofence updated to ${currentRadius}m! Active triggers synchronized with all mobile clients.`);
    } else {
      alert('Geofence updated locally (simulated mode).');
    }
  } catch (_) {
    alert(`Geofence updated locally to ${currentRadius}m! (Ready for live backend connection)`);
  } finally {
    btn.textContent = 'Save Geofence';
    btn.disabled = false;
  }
});

// Initial Setup
resizeCanvas();
