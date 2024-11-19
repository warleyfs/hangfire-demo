import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
  vus: 10,
  duration: '30s',
};

export default function() {
  const url = 'http://localhost/job';
  const payload = JSON.stringify({
    jobCount: 1,
    delay: "00:05:00",
    forceRetry: true,
  });
  const params = {
    headers: { 
      'Content-Type': 'application/json'
    },
  }
  http.post(url, payload, params);
  sleep(1);
}
