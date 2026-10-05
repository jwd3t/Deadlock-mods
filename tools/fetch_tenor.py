import urllib.request
import re

url = "https://tenor.com/es-419/view/%E5%8D%B1-sekiro-%E5%8D%B1-danger-dangerous-sekiro-gif-5056224072908633413"
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'})
try:
    with urllib.request.urlopen(req) as resp:
        html = resp.read().decode('utf-8')
        matches = set(re.findall(r'https://media\.tenor\.com/[^"\'>< ]+', html))
        for m in sorted(matches):
            if any(ext in m for ext in ['.gif', '.mp4', '.webm', '.png']):
                print(m)
except Exception as e:
    print("Error:", e)
