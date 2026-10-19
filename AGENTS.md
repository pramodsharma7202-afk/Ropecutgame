# RopeCutGame

2D mobile rope-cut puzzle game, Unity, portrait 1080x1920, C# only, Rigidbody2D/HingeJoint2D physics, ScriptableObject level data, cartoon style with no gore. After every change check the console and fix errors before reporting done.

HARD RULES:
- Never call console_log more than 3 times in a row without changing something.
- Never log inside a loop. One summary log per level maximum.
- Max 15 minutes per task. If not finished, stop and report what blocks you.
- Never create editor scripts that run for more than 60 seconds.
- After any compile error, fix it first before anything else.

- NEVER edit or compile scripts while Play mode is running. Exit Play mode first, wait until
  editor_state shows not playing and not compiling, then edit.
- After any compile, call unity_list_instances once; if the port changed, select the new one. Do not poll in a loop.