# LOS SANTOS INTERNET RADIO LS
## Manual do usuário - v1.0.2 BETA TEST

**Idioma:** Português (Brasil)  
**Base testada:** v7.41  
**Uso:** GTA V Singleplayer

> **Importante:** este mod foi feito exclusivamente para GTA V Singleplayer. Se uma sessão online/de rede for detectada, as funções do mod serão desativadas.

## Sumário

1. Início rápido
2. Controles
3. HOME e fontes de áudio
4. Internet Radio
5. Spotify e YouTube Music
6. Favoritos
7. Menu CAR / Veículo
8. Luzes e luz da placa
9. Turbo Blow-Off
10. HUD, velocímetro e indicadores
11. Configurações e salvamento
12. Instalação, atualização e desinstalação
13. Solução de problemas
14. Beta Test: o que informar

---

## 1. Início rápido

1. Instale **ScriptHookV** e **ScriptHookVDotNet**.
2. Copie a pasta completa `scripts` do pacote para a pasta principal do GTA V.
3. Garanta que a conta do Windows usada para executar GTA V tenha permissão de **leitura/gravação/modificação** em `GTA V/scripts/InternetRadio/`.
4. Inicie GTA V em **Singleplayer**.
5. Entre em um veículo compatível.
6. Pressione `NUM0` para abrir o menu multimídia.
7. Navegue com `NUM8 / NUM2` e selecione com `NUM5`.

No primeiro uso, `scripts/InternetRadio/UserSettings.ini` é criado automaticamente. As preferências pessoais ficam separadas da configuração principal.

### Configuração de referência desta beta

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Outras versões compatíveis de SHVDN podem funcionar. Se houver erro de compilação, confira primeiro se `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` e `ScriptHookVDotNet3.dll` vieram do **mesmo pacote de versão**.

## 2. Controles

| Tecla | Função |
|---|---|
| `NUM0` | Abrir / fechar menu multimídia |
| `NUM8 / NUM2` | Mover seleção; faixa anterior/próxima em Spotify/YouTube |
| `NUM4 / NUM6` | Trocar aba; estação/fonte anterior/próxima fora do menu |
| `NUM5` | Selecionar / aplicar / ativar fonte |
| `NUM1` | Fonte ativa on/off ou Play/Pause |
| `NUM3` | Stop/Pause da fonte ativa |
| `NUM- / NUM+` | Diminuir / aumentar volume |
| `NUM7` | Pisca-alerta on/off |
| `NUM9` | Farol alto manual on/off |
| `ESPAÇO` | Adicionar/remover estação dos favoritos |
| `DELETE` | Remover favorito selecionado |
| `F8` | Recarregar configuração |
| `ESC / BACK` | Fechar menu |

`Num Lock` deve estar ligado.

## 3. HOME e fontes de áudio

HOME é a central do sistema. A partir dela você acessa Internet Radio, GTA Radio, YouTube Music, Spotify, CAR/Veículo, skins, configurações e informações.

A última fonte ativa é restaurada quando possível após trocar de veículo ou reiniciar. Internet Radio, GTA Radio, Spotify e YouTube Music são controlados separadamente.

## 4. Internet Radio

Escolha estações por categoria. Título e artista aparecem quando o stream fornece metadados.

Os streams são operados por terceiros. Uma estação pode ficar offline, mudar de URL ou ser bloqueada por região sem relação com o mod. Se uma estação falhar, teste outra primeiro.

## 5. Spotify e YouTube Music

Spotify e YouTube Music usam a sessão de mídia disponível do Windows/aplicativo.

1. Abra Spotify ou YouTube Music.
2. Inicie uma faixa.
3. Abra a aba correspondente no menu LS.
4. Pressione `NUM5` para ativar a fonte.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = faixa anterior/próxima.

O painel da direita mostra conexão e volume do aplicativo. **NÃO CONECTADO** significa que nenhuma sessão de mídia compatível foi detectada.

## 6. Favoritos

Pressione `ESPAÇO` em uma estação para adicionar ou remover dos favoritos. São suportados até **6 favoritos**. `DELETE` remove o favorito selecionado.

Os favoritos ficam salvos nas configurações do usuário e permanecem depois de reiniciar.

## 7. Menu CAR / Veículo

A aba **CAR / VEÍCULO** contém recursos opcionais de conforto, iluminação e exibição, dependendo do veículo:

- Setas automáticas
- Pisca-alerta com `NUM7`
- Farol alto com `NUM9`
- Beat Neon
- Luz interna/cabine
- Luz da placa
- Turbo Blow-Off
- Velocímetro / Mini HUD
- Logo da fabricante
- RPM e indicadores do veículo

Algumas classes especiais podem intencionalmente não usar todos os recursos.

## 8. Luzes e luz da placa

v1.0.2 Beta Test usa a lógica v7.41 testada:

- A luz da placa permanece **DESLIGADA durante a luz normal do dia**.
- Luz ambiente, sombras, DRL ou estados automáticos do GTA não a ligam sozinhos.
- Um comando real de luz do motorista pode ativá-la durante o dia.
- À noite, ela acompanha farol baixo/alto reais.
- Mudanças rápidas de estado são filtradas para evitar piscadas.
- `PLATE LIGHT` precisa estar ativado no menu CAR.

O neon estático pode fornecer a cor da luz da placa. Sem neon estático, a lâmpada segue a família de cor dos faróis normais/xenon. Beat Neon não faz a luz da placa pulsar.

## 9. Turbo Blow-Off

**Turbo Blow-Off** é propositalmente apenas `ON / OFF`.

Quando ativado, um único efeito mais forte no estilo SPORT é acionado após carga real do turbo:

- ao subir uma marcha, ou
- ao soltar claramente o acelerador depois do boost.

A lógica anti-spam evita repetições. A detecção também funciona em alta velocidade e no ar. Modelos add-on extras podem ser adicionados em `InternetRadio.ini` com `FactoryTurboModels=`.

## 10. HUD, velocímetro e indicadores

Conforme a classe do veículo:

- Veículos de rua: velocidade / RPM / dados do veículo
- Barcos: HUD marítimo
- Aviões e helicópteros: HUD de voo

A velocidade usa a velocidade real do veículo no GTA. Os logos das fabricantes usam texturas HUD do próprio GTA e um símbolo neutro é usado quando não existe um logo compatível.

## 11. Configurações e salvamento

As preferências pessoais são salvas automaticamente em:

`GTA V/scripts/InternetRadio/UserSettings.ini`

Isso inclui diversas configurações de UI, áudio, veículo, favoritos, última fonte e última estação.

`InternetRadio.ini` contém a configuração principal, lista de estações, padrões técnicos e opções de add-ons. Faça backup de alterações manuais antes de atualizar.

## 12. Instalação, atualização e desinstalação

### Instalação nova

Copie a pasta completa `scripts` do pacote beta para a pasta principal do GTA V.

### Atualização

1. Feche GTA V.
2. Opcionalmente faça backup de `scripts/InternetRadio/UserSettings.ini`.
3. Copie os novos arquivos `scripts` sobre os antigos.
4. Mantenha `UserSettings.ini` para preservar as preferências pessoais.
5. Inicie GTA V e teste o mod.

### Desinstalação

Remova `scripts/03_InternetRadioSimple.3.cs` e `scripts/InternetRadio/`. Não apague outros mods da pasta `scripts`.

## 13. Solução de problemas

**O menu não abre:** confira ScriptHookV, ScriptHookVDotNet, modo Singleplayer e se você está em um veículo compatível.

**Erro de compilação C#:** abra `ScriptHookVDotNet.log`, confira a versão SHVDN e se todos os arquivos SHVDN são do mesmo pacote. Referência testada: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**A rádio não toca:** teste outra estação; streams de terceiros podem estar offline ou bloqueados por região.

**Spotify/YouTube mostra NÃO CONECTADO:** inicie a reprodução no app/navegador primeiro e confira se o Windows detecta uma sessão de mídia.

**Configurações não são salvas:** confira permissões de leitura/gravação/modificação em `GTA V/scripts/InternetRadio/`.

**A luz da placa não acende:** ative `PLATE LIGHT` e ligue as luzes reais do veículo.

**A luz da placa acende de dia ou pisca:** informe veículo, horário, posição das luzes e, se possível, envie um vídeo curto.

**Alterações no INI não funcionam:** pressione `F8` ou reinicie GTA V.

## 14. Beta Test: o que informar

Inclua, se possível:

- GTA V **Enhanced ou Legacy**
- versão do ScriptHookVDotNet
- nome do veículo / spawn name do add-on
- fonte ativa
- configuração ativa
- passos exatos para reproduzir
- captura/vídeo para problemas de UI ou iluminação
- linhas relevantes de `ScriptHookVDotNet.log`

Áreas prioritárias da v1.0.2: painel direito da UI, luz da placa, Turbo Blow-Off, persistência das configurações e status de conexão Spotify/YouTube.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
Modificação não oficial para GTA V Singleplayer.
