import re

with open(__import__('os').path.join(__import__('os').path.dirname(__file__), '..', 'dumps', 'dump_laco.txt'), 'r', encoding='utf-8', errors='ignore') as f:
    text = f.read()

matches = re.findall(r'\"([^\"]*(?:death|dead|respawn|screen_effect|spectat)[^\"]*)\"', text, re.IGNORECASE)
for m in sorted(set(matches)):
    print(m)
