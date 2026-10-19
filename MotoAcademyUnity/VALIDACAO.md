# Validação

Unity 6000.3.0f1 · Windows x64 · Mono · 19/10/2026.

- Importação do projeto e compilação do executável concluídas.
- 21 verificações do núcleo aprovadas: partida, marchas, embreagem, motor e pontuação.
- 15 verificações de integração aprovadas no executável, sem exceções registradas e com encerramento normal (código 0).
- Menu, pilotagem e chuva inspecionados em 1280×720 na Intel UHD Graphics.

O relatório está em [QA/runtime-report.json](QA/runtime-report.json), acompanhado das capturas de tela. A integração cobre entrada pelo Input System, suspensão, arrancada, direção, frenagem, pisos, rampa, chuva e salvamento.

Para repetir os testes em PowerShell 7:

```powershell
.\Tools\Test-Core.ps1
.\Tools\Test-Windows.ps1 -Visible
```

O segundo comando requer a build em `Builds/Windows/`. Os eventos de teclado são gerados pelo ensaio. Controle Xbox físico, todas as aulas e o percurso completo da avaliação ainda precisam de testes manuais. Não há medição de FPS nem calibração com dados de uma motocicleta real.
