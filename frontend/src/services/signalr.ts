import * as signalR from '@microsoft/signalr';

// Auto-detect SignalR URL trong development
function getHubUrl(): string {
  // Nếu có env variable, dùng nó
  if (import.meta.env.VITE_SIGNALR_HUB) {
    return import.meta.env.VITE_SIGNALR_HUB;
  }
  
  // Development: tự động detect từ window.location
  if (import.meta.env.DEV) {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    // Nếu là localhost, dùng localhost:8080
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      return 'http://localhost:8080/hub/game';
    }
    // Nếu là IP khác, dùng cùng IP với port 8080
    return `${protocol}//${hostname}:8080/hub/game`;
  }
  
  // Production: dùng config
  return import.meta.env.VITE_SIGNALR_HUB || 'http://localhost:8080/hub/game';
}

const hubUrl = getHubUrl();

export const hub = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    accessTokenFactory: () => {
      const token = localStorage.getItem('token');
      // SignalR will add this as query parameter ?access_token=...
      return token || '';
    }
  })
  .withAutomaticReconnect()
  .build();

// Log connection errors and state changes
hub.onclose((error) => {
  console.error('SignalR connection closed:', error);
  window.dispatchEvent(new CustomEvent('signalr:closed', { detail: error }));
});

hub.onreconnecting((error) => {
  console.log('SignalR reconnecting:', error);
  window.dispatchEvent(new CustomEvent('signalr:reconnecting', { detail: error }));
});

hub.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  window.dispatchEvent(new CustomEvent('signalr:reconnected', { detail: connectionId }));
});


