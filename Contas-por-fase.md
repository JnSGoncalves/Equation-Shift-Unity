# Contas por fase de Equation Shift

A progressão começa em **G2** e inclui os desvios para coletar as formas e acionar as alavancas. G1 é a vila de introdução e 00 e A0 são salas de exemplo. As primeiras contas pedem poucos empurrões. As seguintes combinam operadores, números de dois dígitos e mais blocos móveis. As cinco salas de obtenção têm uma conta curta para ensinar a nova forma.

## Ordem de progressão

1. **Igual** — G2 → G3 → H3. Pegue Igual em H3, use-o para voltar a G3 e siga por G4 e G5 até G6.
2. **Mais** — G6 → F6 → E6 → D6. Pegue Mais em D6 e use-o para voltar a E6. Retorne por F6 a G6 e abra a passagem para G7 com Mais.
3. **Menos** — Pegue Menos em G7 e use-o para sair rumo a G6. Volte por F6 a E6, use Menos para abrir E5 e depois use Mais para chegar a E4. Siga por D4 e C4 até B4.
4. **Vezes** — De B4, vá a B5 e pegue Vezes. Use-o na saída para B6 e, depois, na passagem de B7 para C7.
5. **Divisão e alavancas** — De C7, desça a D7, pegue Divisão e use-a para voltar a C7. Abra C6 com Divisão e acione sua alavanca. Volte por C7, B7, B6, B5 e B4 até B3. Use Divisão para chegar a B2, depois Vezes para chegar a C2 e acionar a segunda alavanca. Volte a B4 por B2 e B3.
6. **Fase final** — Entre em A4 pelo portão superior de B4. Resolva as quatro contas menores para liberar os operadores da conta principal.

## Contas por portão

Cada caractere da conta ocupa uma célula. Os números entre parênteses na coluna de fixos indicam a posição na conta, contando a partir de **1**. Por exemplo, em `15-6=9`, o `1` e o `5` ocupam as células 1 e 2. Cada conta usa o valor da âncora que já existe na cena. A coluna de móveis lista todas as peças empurráveis necessárias para a solução proposta.

Quando há uma transformação obrigatória, o Player ocupa a célula indicada com o próprio símbolo. No Unity, use `*` para Vezes e `/` para Divisão. O símbolo do Player fica fora da lista de blocos móveis.

| Fase | Portão e destino | Conta a montar | Blocos fixos sugeridos | Blocos móveis | Transformação necessária | Observação |
| --- | --- | --- | --- | --- | --- | --- |
| G2 | Direita → G3 | `2+4=6` | `2` (1), `+` (2), `=` (4), `6` (5, âncora) | `4` | Nenhuma | — |
| G3 | Baixo → H3 | `7-4=3` | `7` (1), `=` (4), `3` (5, âncora) | `-`, `4` | Nenhuma | Desvio para obter Igual. |
| H3 | Topo → G3 | `9=9` | `9` (1, âncora), `9` (3) | — | Igual `=` na célula 2 | Obtenção e tutorial. |
| G3 | Direita → G4 | `2+3=5` | `2` (1, âncora), `+` (2), `5` (5) | `3` | Igual `=` na célula 4 | — |
| G4 | Direita → G5 | `3+4-2=5` | `3` (1, âncora), `+` (2), `=` (6), `5` (7) | `4`, `-`, `2` | Nenhuma | — |
| G5 | Esquerda → G4 | `1+4=5` | `1` (1, âncora), `+` (2), `=` (4), `5` (5) | `4` | Nenhuma | Opcional. O portão começa aberto na cena atual. |
| G5 | Direita → G6 | `15-6+2=11` | `1` (1), `5` (2, âncora), `+` (5), `=` (7), `1` (9) | `-`, `6`, `2`, `1` | Nenhuma | — |
| G6 | Topo → F6 | `3*2+4=10` | `3` (1, âncora), `+` (4), `=` (6), `0` (8) | `*`, `2`, `4`, `1` | Nenhuma | Desvio para obter Mais. |
| F6 | Topo → E6 | `8+7*2=22` | `8` (1, âncora), `+` (2), `=` (6), `2` (8) | `7`, `*`, `2` ×2 | Nenhuma | — |
| E6 | Topo → D6 | `3*4+5=17` | `3` (1, âncora), `+` (4), `=` (6), `1` (7) | `*`, `4`, `5`, `7` | Nenhuma | — |
| D6 | Baixo → E6 | `3+5=8` | `3` (1), `5` (3), `=` (4), `8` (5, âncora) | — | Mais `+` na célula 2 | Obtenção e tutorial. |
| G6 | Direita → G7 | `3+5*2=13` | `3` (1, âncora), `*` (4), `=` (6), `1` (7) | `5`, `2`, `3` | Mais `+` na célula 2 | Voltar de D6 via E6 e F6. |
| G7 | Esquerda → G6 | `7-3=4` | `7` (1), `3` (3), `=` (4), `4` (5, âncora) | — | Menos `-` na célula 2 | Obtenção e tutorial. |
| E6 | Esquerda → E5 | `19-3*4+2=9` | `1` (1), `9` (2, âncora), `*` (5), `=` (9), `9` (10) | `3`, `4`, `+`, `2` | Menos `-` na célula 3 | — |
| E5 | Esquerda → E4 | `5+6*2-3=14` | `5` (1, âncora), `*` (4), `-` (6), `=` (8), `1` (9) | `6`, `2`, `3`, `4` | Mais `+` na célula 2 | — |
| E4 | Topo → D4 | `3*7-8+4=17` | `3` (1, âncora), `-` (4), `=` (8), `7` (10) | `*`, `7`, `8`, `+`, `4`, `1` | Nenhuma | — |
| D4 | Topo → C4 | `18-4*2+3=13` | `8` (2, âncora), `-` (3), `*` (5), `=` (9), `3` (11) | `1` ×2, `4`, `2`, `+`, `3` | Nenhuma | — |
| C4 | Topo → B4 | `27-3*5+2=14` | `2` (1), `7` (2, âncora), `*` (5), `=` (9), `4` (11) | `-`, `3`, `5`, `+`, `2`, `1` | Nenhuma | — |
| B4 | Topo → A4 | — | — | — | Nenhuma | Acionar as alavancas de C2 e C6. |
| B5 | Direita → B6 | `2*5=10` | `2` (1), `5` (3, âncora), `=` (4), `1` (5), `0` (6) | — | Vezes `*` na célula 2 | Obtenção e tutorial. |
| B6 | Direita → B7 | `5*4-3+2=19` | `5` (1, âncora), `*` (2), `-` (4), `=` (8), `9` (10) | `4`, `3`, `+`, `2`, `1` | Nenhuma | — |
| B7 | Baixo → C7 | `12+3*4-5=19` | `1` (1, âncora), `2` (2), `+` (3), `-` (7), `=` (9), `9` (11) | `3`, `4`, `5`, `1` | Vezes `*` na célula 5 | — |
| D7 | Topo → C7 | `9/3=3` | `9` (1, âncora), `3` (3), `=` (4), `3` (5) | — | Divisão `/` na célula 2 | Obtenção e tutorial. |
| C7 | Esquerda → C6 | `9/3+4*2-1=10` | `9` (1, âncora), `+` (4), `*` (6), `-` (8), `=` (10), `0` (12) | `3`, `4`, `2`, `1` ×2 | Divisão `/` na célula 2 | Alavanca de C6. |
| B3 | Esquerda → B2 | `45/5+3*4-2=19` | `4` (1), `5` (2, âncora), `+` (5), `*` (7), `-` (9), `=` (11), `9` (13) | `5`, `3`, `4`, `2`, `1` | Divisão `/` na célula 3 | Voltar de C6 pelo caminho de B4. |
| B2 | Baixo → C2 | `24-3*5+18/6=12` | `2` (1), `4` (2, âncora), `-` (3), `+` (7), `/` (10), `=` (12), `2` (14) | `3`, `5`, `1` ×2, `8`, `6` | Vezes `*` na célula 5 | Alavanca de C2. |
| A4 | `Gate_Horizontal_1` | `5=10/2` | `5` (1, `_Ancor_1`), `=` (2) | `1`, `0`, `2` | Divisão `/` na célula 5 da solução | Libera o bloco `/`. |
| A4 | `Gate_Horizontal_2` | `9=8+1` | `9` (1, `_Ancor_2`), `=` (2) | `8`, `1` | Mais `+` na célula 4 da solução | Libera o bloco `+`. |
| A4 | `Gate_Horizontal_3` | `2*2=4` | `=` (4), `4` (5, `_Ancor_3`) | `2` ×2 | Vezes `*` na célula 2 da solução | Libera o bloco `*`. |
| A4 | `Gate_Horizontal_4` | `8-2=6` | `=` (4), `6` (5, `_Ancor_4`) | `8`, `2` | Menos `-` na célula 2 da solução | Libera o bloco `-`. |
| A4 | `Gate_Horizontal_D` → B4 | `48/2+11=20+2*9-3` | `4` (1), `1` (7, `_Ancor_D1`), `2` (9, `_Ancor_D2`), `+` (11), `9` (14), `3` (16) | `8`, `/`, `2` ×2, `+`, `1`, `0`, `*`, `-` | Igual `=` na célula 8 da solução | Os dois lados valem 35. As duas âncoras precisam estar na mesma conta verdadeira. |

Em A4, reutilize os dígitos móveis nas contas menores e na principal. As formas da tabela preenchem as lacunas da solução proposta. Os portões numerados verificam a participação da âncora de mesmo número em uma conta verdadeira.

## Montagem nas cenas

- **Salas sem portão de conta:** C2 e C6 têm as alavancas. O portão superior de B4 abre com as duas ativações. A passagem inferior de C7 para D7 é livre para que o jogador possa buscar Divisão.
- **Coletáveis:** Igual em H3, Mais em D6, Menos em G7, Vezes em B5 e Divisão em D7.
- **Obrigatoriedade das formas:** os 13 portões da tabela com transformação exigem que a equação inclua a âncora e o Player na forma indicada. A abertura temporária do portão permite entrar na sala. A saída pelo portal é liberada quando a conta é resolvida.
- **Blocos e âncoras:** os fixos marcam a linha horizontal da conta. Nas fases numeradas, os móveis começam deslocados na coluna da célula correspondente e podem ser empurrados pela parte de cima ou de baixo, conforme a sala.
- Os nomes `<código>-Fase_<número>` seguem a ordem da primeira visita, de `G1-Fase_1` a `C2-Fase_25`. O Build inclui as cenas desse percurso e `A4-Fase_Final`. A versão de C6 com a alavanca é `C6-Fase_22`.
