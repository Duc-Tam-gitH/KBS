import json
from pathlib import Path
from graphify.cache import save_semantic_cache
from graphify.build import build_from_json
from graphify.cluster import cluster, score_all
from graphify.analyze import god_nodes, surprising_connections, suggest_questions
from graphify.report import generate
from graphify.export import to_json
from graphify.diagnostics import diagnose_extraction, format_diagnostic_report

OUT = Path('graphify-out')
ROOT = 'T:/KBS'
SPEC = 'C:/Users/mytaa/.codex/skills/graphify/references/extraction-spec.md'
def read(name):
    return json.loads((OUT / name).read_text(encoding='utf-8'))
def write(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

chunks = [read(f'.graphify_chunk_{i:02}.json') for i in range(1, 7)]
for c in chunks:
    assert isinstance(c.get('nodes'), list) and isinstance(c.get('edges'), list)
    assert isinstance(c.get('hyperedges', []), list)
covered = {}
for c in chunks:
    covered.update(c.get('coverage', {}))
partial = []
for src, count in covered.items():
    if Path(src).suffix.lower() in ('.png', '.jpg', '.jpeg'):
        continue
    total = len(Path(src).read_text(encoding='utf-8', errors='replace').splitlines())
    if count < total:
        partial.append(src)
        print('PARTIAL', src, count, total)
(OUT / '.graphify_partial.txt').write_text('\n'.join(partial), encoding='utf-8')
sem = {key: [item for c in chunks for item in c.get(key, [])] for key in ('nodes','edges','hyperedges')}
sem.update(input_tokens=0, output_tokens=0, token_usage_available=False)
write('.graphify_semantic_new.json', sem)
uncached = (OUT / '.graphify_uncached.txt').read_text(encoding='utf-8').splitlines()
saved = save_semantic_cache(sem['nodes'], sem['edges'], sem['hyperedges'], root=ROOT,
    allowed_source_files=uncached, prompt_file=SPEC, partial_source_files=partial)
print('Cached', saved, 'files. Agent token usage not exposed by tool.')
cached = read('.graphify_cached.json') if (OUT / '.graphify_cached.json').exists() else {}
for key in ('nodes','edges','hyperedges'):
    sem[key] = cached.get(key, []) + sem[key]
sem['nodes'] = list({n['id']: n for n in sem['nodes']}.values())
for item in sem['nodes'] + sem['edges']:
    item['_origin'] = 'semantic'
write('.graphify_semantic.json', sem)
ast = read('.graphify_ast.json')
nodes = {n['id']: n for n in ast['nodes']}
for node in sem['nodes']:
    nodes.setdefault(node['id'], node)
extraction = dict(nodes=list(nodes.values()), edges=ast['edges']+sem['edges'],
    hyperedges=sem['hyperedges'], input_tokens=0, output_tokens=0, token_usage_available=False)
write('.graphify_extract.json', extraction)
G = build_from_json(extraction, root=ROOT, directed=False)
assert G.number_of_nodes(), 'Empty graph'
communities = cluster(G)
cohesion = score_all(G, communities)
gods = god_nodes(G)
surprises = surprising_connections(G, communities)
labels = {cid: 'Community '+str(cid) for cid in communities}
questions = suggest_questions(G, communities, labels)
assert to_json(G, communities, str(OUT/'graph.json')), 'Shrink guard refused graph'
report = generate(G, communities, cohesion, labels, gods, surprises, read('.graphify_detect.json'),
    {'input': 0, 'output': 0}, ROOT, suggested_questions=questions)
(OUT/'GRAPH_REPORT.md').write_text(report, encoding='utf-8')
write('.graphify_analysis.json', dict(communities=communities, cohesion=cohesion,
    gods=gods, surprises=surprises, questions=questions))
health = diagnose_extraction(extraction, directed=False, root=ROOT)
write('health.json', health)
print(format_diagnostic_report(health))
print('Graph:', G.number_of_nodes(), 'nodes', G.number_of_edges(), 'edges', len(communities), 'communities')
for cid, members in communities.items():
    top = sorted(members, key=lambda n: G.degree(n), reverse=True)[:8]
    print(cid, len(members), [G.nodes[n].get('label', n) for n in top])
