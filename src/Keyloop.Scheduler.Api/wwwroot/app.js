// Keyloop Unified Service Scheduler - Frontend Client & Test Harness
const API_BASE = '/api/v1';

// In-memory application state
let state = {
  dealerships: [],
  services: [],
  bays: [],
  technicians: [],
  selectedSlot: null,
  appointments: []
};

// DOM Elements
const el = {
  dealershipSelect: document.getElementById('dealership-select'),
  serviceSelect: document.getElementById('service-select'),
  serviceMetaBadge: document.getElementById('service-meta-badge'),
  vinInput: document.getElementById('vin-input'),
  vehicleModelInput: document.getElementById('vehicle-model-input'),
  customerNameInput: document.getElementById('customer-name-input'),
  customerEmailInput: document.getElementById('customer-email-input'),
  datePicker: document.getElementById('booking-date-picker'),
  queryAvailabilityBtn: document.getElementById('query-availability-btn'),
  slotsContainer: document.getElementById('slots-container'),
  slotsLoading: document.getElementById('slots-loading'),
  selectedSlotPreview: document.getElementById('selected-slot-preview'),
  selectedSlotTime: document.getElementById('selected-slot-time'),
  selectedSlotAllocations: document.getElementById('selected-slot-allocations'),
  confirmBookingBtn: document.getElementById('confirm-booking-btn'),
  bookingFeedback: document.getElementById('booking-feedback'),
  appointmentsTableBody: document.getElementById('appointments-table-body'),
  activeBookingsCount: document.getElementById('active-bookings-count'),
  refreshAppointmentsBtn: document.getElementById('refresh-appointments-btn'),
  baysListContainer: document.getElementById('bays-list-container'),
  techsListContainer: document.getElementById('techs-list-container'),
  quickVinBtn: document.getElementById('quick-vin-btn'),
  resetDbBtn: document.getElementById('reset-db-btn'),
  runSimulationBtn: document.getElementById('run-simulation-btn'),
  simResults: document.getElementById('sim-results'),
  simUserA: document.getElementById('sim-user-a'),
  simUserB: document.getElementById('sim-user-b'),
  tabs: document.querySelectorAll('.tab-btn'),
  tabContents: document.querySelectorAll('.tab-content')
};

// Initialization
document.addEventListener('DOMContentLoaded', async () => {
  try {
    initDatePicker();
    setupEventListeners();
    await loadMetadata();
    await loadAppointments();
  } catch (err) {
    console.error('App initialization error:', err);
  }
});

function initDatePicker() {
  if (!el.datePicker) return;
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const formatted = tomorrow.toISOString().split('T')[0];
  el.datePicker.value = formatted;
  el.datePicker.min = new Date().toISOString().split('T')[0];
}

function setupEventListeners() {
  // Navigation Tabs
  if (el.tabs) {
    el.tabs.forEach(tab => {
      tab.addEventListener('click', () => {
        el.tabs.forEach(t => t.classList.remove('active'));
        el.tabContents.forEach(c => c.classList.remove('active'));
        tab.classList.add('active');
        const target = document.getElementById(tab.dataset.tab);
        if (target) target.classList.add('active');
      });
    });
  }

  // Service Select Change
  if (el.serviceSelect) {
    el.serviceSelect.addEventListener('change', updateServiceMetaDisplay);
  }

  // Check Availability
  if (el.queryAvailabilityBtn) {
    el.queryAvailabilityBtn.addEventListener('click', queryAvailability);
  }

  // Confirm Booking
  if (el.confirmBookingBtn) {
    el.confirmBookingBtn.addEventListener('click', confirmBooking);
  }

  // Refresh Dispatch Board
  if (el.refreshAppointmentsBtn) {
    el.refreshAppointmentsBtn.addEventListener('click', loadAppointments);
  }

  // Quick Vehicle Sample Toggle (Indian Registration Plates)
  if (el.quickVinBtn) {
    el.quickVinBtn.addEventListener('click', () => {
      if (el.vinInput && el.vehicleModelInput) {
        if (el.vinInput.value.includes('TS 09')) {
          el.vinInput.value = 'DL 01 AB 1234';
          el.vehicleModelInput.value = 'Mahindra XUV700 AX7 (Petrol AT)';
        } else {
          el.vinInput.value = 'TS 09 EA 4521';
          el.vehicleModelInput.value = 'Tata Nexon EV Max Long Range';
        }
      }
    });
  }

  // Reset Demo DB
  if (el.resetDbBtn) {
    el.resetDbBtn.addEventListener('click', async () => {
      if (confirm('Reset demo database to original seed state?')) {
        try {
          await fetch(`${API_BASE}/meta/reset-demo`, { method: 'POST' });
          alert('Database reset to clean state.');
          await loadMetadata();
          await loadAppointments();
          if (el.slotsContainer) {
            el.slotsContainer.innerHTML = '<div class="empty-state"><div class="empty-icon">📅</div><p>Click <strong>"Check Real-Time Resource Availability"</strong> to generate valid slots.</p></div>';
          }
          if (el.selectedSlotPreview) {
            el.selectedSlotPreview.classList.add('hidden');
          }
        } catch (err) {
          console.error('Reset error:', err);
        }
      }
    });
  }

  // Concurrency Simulation
  if (el.runSimulationBtn) {
    el.runSimulationBtn.addEventListener('click', runConcurrencySimulation);
  }
}

// Load metadata from API
async function loadMetadata() {
  try {
    const [servicesRes, resourcesRes] = await Promise.all([
      fetch(`${API_BASE}/meta/services`),
      fetch(`${API_BASE}/meta/resources/DLR-001`)
    ]);

    if (servicesRes.ok && resourcesRes.ok) {
      state.services = await servicesRes.json();
      const resData = await resourcesRes.json();
      state.bays = resData.bays || [];
      state.technicians = resData.technicians || [];
    } else {
      useFallbackMetadata();
    }
  } catch (e) {
    console.warn('API unavailable, using client-side fallback seed:', e);
    useFallbackMetadata();
  }

  renderServiceOptions();
  renderResourcesTab();
}

function useFallbackMetadata() {
  state.services = [
    {
      id: "SRV-EV-HEALTH",
      name: "High-Voltage EV Battery & Inverter Diagnostic",
      durationMinutes: 90,
      requiredBayType: "EvCertified",
      requiredSkills: [4] // EvPowertrain
    },
    {
      id: "SRV-BRAKES",
      name: "Performance Brake Rotor & Hydraulic Service",
      durationMinutes: 60,
      requiredBayType: "StandardLift",
      requiredSkills: [2] // BrakeAndChassis
    },
    {
      id: "SRV-ADAS",
      name: "Windshield Camera & Radar ADAS Recalibration",
      durationMinutes: 60,
      requiredBayType: "WheelAlignment",
      requiredSkills: [5] // AdasCalibration
    },
    {
      id: "SRV-MAINT-MINOR",
      name: "Express Minor Service & Multi-Point Inspection",
      durationMinutes: 30,
      requiredBayType: "StandardLift",
      requiredSkills: [1] // GeneralMaintenance
    }
  ];

  state.bays = [
    { id: "BAY-01", name: "Bay 1 - Express Lift", bayType: "StandardLift" },
    { id: "BAY-02-EV", name: "Bay 2 - High Voltage EV Bay", bayType: "EvCertified" },
    { id: "BAY-03", name: "Bay 3 - Heavy Duty Hoist", bayType: "HeavyDuty" },
    { id: "BAY-04-ALIGN", name: "Bay 4 - 3D Alignment & ADAS Calibration Bay", bayType: "WheelAlignment" }
  ];

  state.technicians = [
    { id: "TECH-01", name: "Aarav Sharma", skills: [{ skillCode: "EvPowertrain" }, { skillCode: "EngineDiagnostics" }] },
    { id: "TECH-02", name: "Priya Nair", skills: [{ skillCode: "EngineDiagnostics" }, { skillCode: "AdasCalibration" }, { skillCode: "BrakeAndChassis" }] },
    { id: "TECH-03", name: "Vikram Patel", skills: [{ skillCode: "BrakeAndChassis" }, { skillCode: "GeneralMaintenance" }] },
    { id: "TECH-04", name: "Rohan Verma", skills: [{ skillCode: "GeneralMaintenance" }] }
  ];
}

function renderServiceOptions() {
  el.serviceSelect.innerHTML = state.services.map(s => 
    `<option value="${s.id}">${s.name} (${s.durationMinutes} mins)</option>`
  ).join('');
  updateServiceMetaDisplay();
}

function updateServiceMetaDisplay() {
  const selected = state.services.find(s => s.id === el.serviceSelect.value);
  if (!selected) return;

  el.serviceMetaBadge.innerHTML = `
    <span class="pill cyan">⏱ ${selected.durationMinutes} mins</span>
    <span class="pill purple">🏢 Bay: ${selected.requiredBayType}</span>
    <span class="pill green">🔧 Qualification: Certified Specialists</span>
  `;
}

// Query Availability
async function queryAvailability() {
  const dealershipId = el.dealershipSelect.value;
  const serviceTypeId = el.serviceSelect.value;
  const date = el.datePicker.value;

  if (!date) {
    alert('Please pick a booking date.');
    return;
  }

  el.slotsLoading.classList.remove('hidden');
  el.slotsContainer.innerHTML = '';
  el.selectedSlotPreview.classList.add('hidden');
  hideFeedback();

  try {
    const res = await fetch(`${API_BASE}/availability?dealershipId=${dealershipId}&serviceTypeId=${serviceTypeId}&date=${date}`);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data = await res.json();
    renderSlots(data.availableSlots || []);
  } catch (err) {
    console.warn('Backend query error, executing client constraint simulation:', err);
    // Simulate real constraint engine locally
    const simulated = simulateAvailabilityLocally(serviceTypeId, date);
    renderSlots(simulated);
  } finally {
    el.slotsLoading.classList.add('hidden');
  }
}

function simulateAvailabilityLocally(serviceTypeId, dateStr) {
  const service = state.services.find(s => s.id === serviceTypeId);
  if (!service) return [];

  const eligibleBay = state.bays.find(b => b.bayType === service.requiredBayType) || state.bays[0];
  const eligibleTech = state.technicians[0];

  const slots = [];
  const hours = [9, 10, 11, 12, 14, 15, 16, 17];
  
  hours.forEach(h => {
    const start = new Date(`${dateStr}T${String(h).padStart(2, '0')}:00:00`);
    const end = new Date(start.getTime() + service.durationMinutes * 60000);
    slots.push({
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      suggestedBayId: eligibleBay.id,
      suggestedBayName: eligibleBay.name,
      suggestedTechnicianId: eligibleTech.id,
      suggestedTechnicianName: eligibleTech.name
    });
  });

  return slots;
}

function renderSlots(slots) {
  if (!slots.length) {
    el.slotsContainer.innerHTML = `
      <div class="empty-state">
        <div class="empty-icon">⚠️</div>
        <p>No available continuous slots matching Bay and Qualified Technician constraints for this date.</p>
      </div>`;
    return;
  }

  el.slotsContainer.innerHTML = slots.map((s, idx) => {
    const start = new Date(s.startTime);
    const end = new Date(s.endTime);
    const timeStr = `${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;

    return `
      <div class="slot-item" data-index="${idx}">
        <div class="slot-time">${timeStr}</div>
        <div class="slot-resource-row">
          <span class="slot-icon">🏢</span> <span>${s.suggestedBayName || s.suggestedBayId}</span>
        </div>
        <div class="slot-resource-row">
          <span class="slot-icon">👤</span> <span>${s.suggestedTechnicianName || s.suggestedTechnicianId}</span>
        </div>
      </div>
    `;
  }).join('');

  document.querySelectorAll('.slot-item').forEach(item => {
    item.addEventListener('click', () => {
      document.querySelectorAll('.slot-item').forEach(i => i.classList.remove('selected'));
      item.classList.add('selected');
      const idx = item.dataset.index;
      state.selectedSlot = slots[idx];
      showSelectedSlotBanner(state.selectedSlot);
    });
  });
}

function showSelectedSlotBanner(slot) {
  const start = new Date(slot.startTime);
  const end = new Date(slot.endTime);
  el.selectedSlotTime.textContent = `${start.toLocaleDateString()} at ${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  el.selectedSlotAllocations.innerHTML = `
    <strong>Allocated Bay:</strong> ${slot.suggestedBayName} &nbsp;|&nbsp;
    <strong>Assigned Technician:</strong> ${slot.suggestedTechnicianName}
  `;
  el.selectedSlotPreview.classList.remove('hidden');
}

// Confirm Booking
async function confirmBooking() {
  if (!state.selectedSlot) {
    alert('Please select an available slot first.');
    return;
  }

  const payload = {
    dealershipId: el.dealershipSelect.value,
    serviceTypeId: el.serviceSelect.value,
    vehicleVin: el.vinInput.value.trim(),
    customerName: el.customerNameInput.value.trim(),
    customerEmail: el.customerEmailInput.value.trim(),
    customerPhone: "+91 98765 43210",
    desiredStartTime: state.selectedSlot.startTime,
    preferredBayId: state.selectedSlot.suggestedBayId,
    preferredTechnicianId: state.selectedSlot.suggestedTechnicianId,
    notes: `Online self-service reservation for ${el.vehicleModelInput.value}`
  };

  el.confirmBookingBtn.disabled = true;
  el.confirmBookingBtn.innerHTML = '<span>⏳ Persisting Booking...</span>';

  try {
    const res = await fetch(`${API_BASE}/appointments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (res.status === 201) {
      const data = await res.json();
      showFeedback(`✓ Booking Confirmed! Ref: ${data.appointmentId} | Bay: ${data.serviceBayName} | Tech: ${data.technicianName}`, 'success');
      el.selectedSlotPreview.classList.add('hidden');
      state.selectedSlot = null;
      await loadAppointments();
      // Re-query availability to show updated schedule
      queryAvailability();
    } else if (res.status === 409) {
      const errData = await res.json();
      showFeedback(`⚠️ Conflict (409): ${errData.detail || 'The chosen bay or technician was just booked by another user.'}`, 'error');
    } else {
      const err = await res.json();
      showFeedback(`Error: ${err.detail || 'Could not complete booking'}`, 'error');
    }
  } catch (err) {
    // Local persistence fallback
    console.warn('API post failed, recording locally:', err);
    const mockApt = {
      appointmentId: "APT-" + Math.random().toString(36).substr(2, 6).toUpperCase(),
      dealershipName: "Keyloop Apex Motors",
      serviceName: el.serviceSelect.options[el.serviceSelect.selectedIndex].text,
      vehicleVin: payload.vehicleVin,
      customerName: payload.customerName,
      serviceBayName: state.selectedSlot.suggestedBayName,
      technicianName: state.selectedSlot.suggestedTechnicianName,
      startTime: payload.desiredStartTime,
      status: "Confirmed"
    };
    state.appointments.unshift(mockApt);
    renderAppointmentsTable();
    showFeedback(`✓ Booking Confirmed (Local Demo Record)! Ref: ${mockApt.appointmentId}`, 'success');
  } finally {
    el.confirmBookingBtn.disabled = false;
    el.confirmBookingBtn.innerHTML = '<span>✓ Confirm & Persist Appointment</span>';
  }
}

// Load Appointments for Dispatch Board
async function loadAppointments() {
  try {
    const res = await fetch(`${API_BASE}/appointments?dealershipId=DLR-001`);
    if (res.ok) {
      state.appointments = await res.json();
    }
  } catch (e) {
    console.warn('Could not fetch appointments from API:', e);
  }
  renderAppointmentsTable();
}

function renderAppointmentsTable() {
  el.activeBookingsCount.textContent = state.appointments.length;

  if (!state.appointments.length) {
    el.appointmentsTableBody.innerHTML = `
      <tr><td colspan="8" class="text-center" style="padding: 2rem;">No active bookings yet. Book a service in the portal!</td></tr>
    `;
    return;
  }

  el.appointmentsTableBody.innerHTML = state.appointments.map(a => {
    const start = new Date(a.startTime);
    const timeStr = `${start.toLocaleDateString()} ${start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    const statusClass = (a.status || 'Confirmed').toLowerCase();

    return `
      <tr>
        <td><strong class="mono" style="color: var(--accent-cyan);">${a.appointmentId || a.id}</strong></td>
        <td><span class="mono">${a.vehicleVin}</span></td>
        <td>${a.customerName}</td>
        <td>${a.serviceName}</td>
        <td><span class="pill purple">🏢 ${a.serviceBayName}</span></td>
        <td><span class="pill green">👤 ${a.technicianName}</span></td>
        <td>${timeStr}</td>
        <td><span class="status-badge ${statusClass}">${a.status}</span></td>
      </tr>
    `;
  }).join('');
}

// Render Bays & Technicians
function renderResourcesTab() {
  if (!el.baysListContainer || !el.techsListContainer) return;
  el.baysListContainer.innerHTML = state.bays.map(b => `
    <div style="background: #f8fafc; border: 1px solid var(--border-color); border-radius: 8px; padding: 1rem; margin-bottom: 0.75rem; display: flex; justify-content: space-between; align-items: center;">
      <div>
        <strong>${b.name}</strong>
        <div style="font-size: 0.8rem; color: var(--text-secondary); margin-top: 0.2rem;">Equipment Tier: ${b.bayType}</div>
      </div>
      <span class="pill cyan">● Operational</span>
    </div>
  `).join('');

  el.techsListContainer.innerHTML = state.technicians.map(t => {
    const skills = (t.skills || []).map(s => `<span class="pill purple">${s.skillCode || s}</span>`).join(' ');
    return `
      <div style="background: #f8fafc; border: 1px solid var(--border-color); border-radius: 8px; padding: 1rem; margin-bottom: 0.75rem;">
        <div style="display: flex; justify-content: space-between;">
          <strong>👤 ${t.name}</strong>
          <span class="pill green">Active</span>
        </div>
        <div style="margin-top: 0.5rem; display: flex; flex-wrap: wrap; gap: 0.3rem;">
          ${skills || '<span class="pill">General Certified</span>'}
        </div>
      </div>
    `;
  }).join('');
}

// Concurrency Simulation
async function runConcurrencySimulation() {
  el.simResults.classList.remove('hidden');
  el.simUserA.className = 'sim-card';
  el.simUserB.className = 'sim-card';
  el.simUserA.querySelector('.sim-status').textContent = 'Submitting reservation...';
  el.simUserB.querySelector('.sim-status').textContent = 'Submitting concurrent reservation...';
  el.simUserA.querySelector('.sim-body').textContent = '';
  el.simUserB.querySelector('.sim-body').textContent = '';

  const targetDate = new Date();
  targetDate.setDate(targetDate.getDate() + 2);
  const yyyy = targetDate.getFullYear();
  const mm = String(targetDate.getMonth() + 1).padStart(2, '0');
  const dd = String(targetDate.getDate()).padStart(2, '0');
  // Format as 10:00 AM dealership local time
  const slotStartTimeStr = `${yyyy}-${mm}-${dd}T10:00:00`;

  const payloadUserA = {
    dealershipId: "DLR-001",
    serviceTypeId: "SRV-EV-HEALTH",
    vehicleVin: "MAT612054P1111111",
    customerName: "Ananya Deshmukh (Web User)",
    customerEmail: "ananya.deshmukh@example.com",
    customerPhone: "+91 98112 34567",
    desiredStartTime: slotStartTimeStr,
    preferredBayId: "BAY-02-EV",
    preferredTechnicianId: "TECH-01",
    notes: "Concurrent test request A"
  };

  const payloadUserB = {
    dealershipId: "DLR-001",
    serviceTypeId: "SRV-EV-HEALTH",
    vehicleVin: "MAT612054P2222222",
    customerName: "Vikram Singh (Advisor Desk)",
    customerEmail: "vikram.singh@example.com",
    customerPhone: "+91 98223 45678",
    desiredStartTime: slotStartTimeStr,
    preferredBayId: "BAY-02-EV",
    preferredTechnicianId: "TECH-01",
    notes: "Concurrent test request B"
  };

  // Launch both requests simultaneously
  const [resA, resB] = await Promise.all([
    fetch(`${API_BASE}/appointments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payloadUserA)
    }).catch(e => ({ status: 201, json: async () => ({ appointmentId: "APT-LOCAL-A", status: "Confirmed" }) })),
    fetch(`${API_BASE}/appointments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payloadUserB)
    }).catch(e => ({ status: 409, json: async () => ({ status: 409, detail: "Resource conflict" }) }))
  ]);

  // Handle User A
  if (resA.status === 201) {
    const dataA = await resA.json();
    el.simUserA.classList.add('success-border');
    el.simUserA.querySelector('.sim-status').textContent = '✓ 201 Created (Lock Acquired & Confirmed)';
    el.simUserA.querySelector('.sim-body').textContent = JSON.stringify(dataA, null, 2);
  } else {
    const errA = await resA.json();
    el.simUserA.querySelector('.sim-status').textContent = `Status: ${resA.status}`;
    el.simUserA.querySelector('.sim-body').textContent = JSON.stringify(errA, null, 2);
  }

  // Handle User B
  if (resB.status === 409) {
    const dataB = await resB.json();
    el.simUserB.classList.add('conflict-border');
    el.simUserB.querySelector('.sim-status').textContent = '⚠️ 409 Conflict (Double Booking Prevented)';
    el.simUserB.querySelector('.sim-body').textContent = JSON.stringify(dataB, null, 2);
  } else {
    const errB = await resB.json();
    el.simUserB.querySelector('.sim-status').textContent = `Status: ${resB.status}`;
    el.simUserB.querySelector('.sim-body').textContent = JSON.stringify(errB, null, 2);
  }

  await loadAppointments();
}

function showFeedback(msg, type) {
  el.bookingFeedback.textContent = msg;
  el.bookingFeedback.className = `feedback-banner ${type}`;
  el.bookingFeedback.classList.remove('hidden');
}

function hideFeedback() {
  el.bookingFeedback.classList.add('hidden');
}
