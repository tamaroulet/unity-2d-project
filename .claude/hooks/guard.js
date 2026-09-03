#!/usr/bin/env node
/**
 * PreToolUse guard. Reads the hook payload on stdin, exits 2 to block.
 * Enforces the Non-Goals that the 34KB natural-language rules failed to enforce.
 */
'use strict';

let raw = '';
process.stdin.on('data', (c) => (raw += c));
process.stdin.on('end', () => {
  let p;
  try { p = JSON.parse(raw || '{}'); } catch { process.exit(0); }

  const tool = p.tool_name || '';
  const ti = p.tool_input || {};
  const block = (msg) => { console.error('[GUARD] ' + msg); process.exit(2); };

  if (tool === 'Bash' || tool === 'PowerShell') {
    const cmd = String(ti.command || '');
    if (/\brm\s+-rf\s+[/~]/.test(cmd) ||
        /\bgit\s+push\b[^\n]*(--force|\s-f\b)/.test(cmd) ||
        /\bgit\s+reset\s+--hard\b/.test(cmd)) {
      block('Destructive command blocked. Ask the human first: ' + cmd);
    }
    process.exit(0);
  }

  const file = String(ti.file_path || ti.notebook_path || '');
  if (!file) process.exit(0);
  const unix = file.split(String.fromCharCode(92)).join('/');

  // 1. Unity YAML serialization is human/editor territory, never text-edited.
  if (/\.(unity|prefab|asset|meta|asmdef)$/i.test(unix)) {
    block('Do not edit Unity serialized files directly (' + unix.split('/').pop() +
      '). Scenes, prefabs, .asset and .asmdef are changed by a human in the Unity Editor. ' +
      'Deleting an .asmdef is done by the human as part of the assembly merge.');
  }

  // The content rules below are about compiled game code only.
  if (!/\.cs$/i.test(unix)) process.exit(0);

  const content = String(ti.content || ti.new_string || '');
  if (!content) process.exit(0);

  // Editor-only and test code may legitimately use editor APIs and scene lookup.
  if (/\/Editor\//i.test(unix) || /\/Tests?\//i.test(unix)) process.exit(0);

  // 2. Editor-only APIs in runtime code vanish in a WebGL build.
  if (/#if\s+UNITY_EDITOR/.test(content) || /\bAssetDatabase\b/.test(content) || /\bUnityEditor\b/.test(content)) {
    block('#if UNITY_EDITOR / AssetDatabase / UnityEditor in runtime code (' + unix +
      '). This compiles out of the WebGL build and the reference becomes null at runtime. ' +
      'Use [SerializeField] assigned by the human, or Resources.Load.');
  }

  // 3. Runtime object lookup by name/type is the Update()-rebinding anti-pattern.
  if (/\bFindFirstObjectByType\b|\bFindAnyObjectByType\b|\bFindObjectOfType\b|\bGameObject\.Find\s*\(|\btransform\.Find\s*\(/.test(content)) {
    block('Runtime Find/FindObjectByType in ' + unix +
      '. Scene lookup by name or type is banned in runtime scripts. ' +
      'Declare a [SerializeField] field and have the human assign it in the Inspector.');
  }

  process.exit(0);
});
