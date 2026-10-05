with open(__import__('os').path.join(__import__('os').path.dirname(__file__), '..', 'dumps', 'dump_laco.txt'), 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

import re
matches = re.findall(r'"([^"]*(?:rejuv|rebirth|reviv)[^"]*)"', text, re.IGNORECASE)
for m in sorted(set(matches)):
    print(m)
