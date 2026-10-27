import json

with open(r'C:\Users\marco\.gemini\antigravity-cli\brain\a033dcb4-98b6-4c0e-9426-c8c6c4b0a839\.system_generated\logs\transcript_full.jsonl', 'r', encoding='utf-8') as f:
    for line in f:
        data = json.loads(line)
        idx = data.get('step_index')
        if idx in (102, 104):
            tool_calls = data.get('tool_calls', [])
            for tc in tool_calls:
                print(f"=== STEP {idx} ===")
                print(tc['args']['Path'])
                print(tc['args']['Content'])
