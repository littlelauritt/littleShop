import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuración de la prueba
export const options = {
  stages: [
    { duration: '10s', target: 20 }, // Subir a 20 usuarios en 10s
    { duration: '30s', target: 50 }, // Mantener 50 usuarios durante 30s
    { duration: '10s', target: 0 },  // Bajar a 0
  ],
  // Umbrales para aprobar/suspender el test
  thresholds: {
    http_req_duration: ['p(95)<500'], // El 95% de las peticiones deben tardar menos de 500ms
  },
};

export default function () {
  // ⚠️ CAMBIA EL PUERTO POR EL DE TU GATEWAY (En tus capturas era 7167)
  const url = 'https://localhost:7167/api/v1/products';

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    // Ignorar errores de certificado SSL (porque es localhost)
    insecureSkipTLSVerify: true, 
  };

  const res = http.get(url);

  // Verificaciones
  check(res, {
    'status is 200': (r) => r.status === 200,
    'status is 429 (Rate Limit)': (r) => r.status === 429, // Esperamos ver algunos de estos si nos pasamos
  });

  sleep(1);
}