# Moto Academy — Costa Verde

Simulador de motocicleta manual com cenário 3D procedural, interface em português, dez aulas, pilotagem livre e avaliação com sete portais.

## Executar no Windows

Baixe o ZIP da seção Releases, extraia a pasta inteira e abra `MotoAcademy.exe`. Mantenha as bibliotecas e a pasta `MotoAcademy_Data` junto do executável.

## Abrir no Unity

1. Use **Unity 6000.3.0f1** com suporte de build Windows (Mono).
2. Adicione esta pasta no Unity Hub e aguarde a importação dos pacotes.
3. Use **Moto Academy > 1. Configurar projeto** e **2. Abrir cena principal**.
4. Pressione Play. O cenário é construído ao iniciar a cena.

Dependências: URP 17.3.0, Input System 1.17.0 e uGUI 2.0.0. A primeira importação requer internet. Aceite o reinício caso o editor solicite ativar o Input System.

## Controles

| Ação | Teclado | Xbox |
|---|---|---|
| Acelerar | W | RT |
| Embreagem | C | LT |
| Direção | A / D | Analógico esquerdo |
| Freio dianteiro / traseiro | Espaço / S | B / A |
| Reduzir / subir marcha | Q / E | LB / RB |
| Neutro | N | D-pad ↑ |
| Partida | R | X |
| Corta-corrente | K | Y |
| ABS | Z | R3 |
| Câmera | V | L3 |
| Reposicionar | Backspace | View |
| Alternar modo | T | Menu |
| Reiniciar objetivo | P | D-pad → |
| Chuva | F6 | Interface |
| Pausa / ajuda | Esc / F1 | Interface |

Para arrancar: ligue com R em neutro, segure C, engate a primeira com E, acelere com W e solte C aos poucos. Para parar, use os freios e puxe a embreagem antes da baixa rotação. O acelerador e a soltura da embreagem são progressivos no teclado.

## Compilar e testar

Use **Moto Academy > 3. Gerar Windows x64** ou PowerShell:

```powershell
.\Tools\Build-Windows.ps1 -EditorPath 'C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe'
.\Tools\Test-Core.ps1
.\Tools\Test-Windows.ps1 -Visible
```

A build fica em `Builds/Windows/`. Feche o editor desse projeto antes de compilar por linha de comando. Os testes usam PowerShell 7; os resultados estão em [VALIDACAO.md](VALIDACAO.md).

## Sistemas

- Motor de 160 cc, cinco marchas e neutro, embreagem, stall e freio por compressão.
- Suspensão por raycast, direção assistida, inclinação visual, ABS e aderência por piso.
- Pátio, circuito, slalom, rampa, asfalto seco/molhado, cascalho e grama.
- Som de motor sintetizado, chuva, três câmeras, telemetria e minimapa.
- Progresso das aulas e avaliação salvos localmente; sessão exportada em JSON.

Os componentes ficam em `Assets/MotoAcademy/Scripts`; o motor e a pontuação estão em `Core`, sem dependência do Unity. A cena principal é `Assets/MotoAcademy/Scenes/Academy.unity`.

A condução é autoestabilizada e didática. O visual é estilizado e procedural. O simulador não reproduz quedas ou toda a dinâmica de uma motocicleta real.
