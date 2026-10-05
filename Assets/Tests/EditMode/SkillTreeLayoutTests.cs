using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reglas de diseño de los árboles de habilidades (los dos assets reales). La vista dibuja cada nodo a UnitScale píxeles por
/// unidad, sin ajustar el árbol a la pantalla (si es más grande se arrastra y se hace zoom), así que lo que importa es que haya
/// aire: los nodos miden NodeSize px y se pide un espacio libre mínimo entre ellos. Las constantes repiten las de SkillTreeView.
/// </summary>
[TestFixture]
public class SkillTreeLayoutTests
{
    private const float UnitScale = 150f;   // px por unidad (SkillTreeView.UnitScale)
    private const float NodeSize = 104f;    // px (SkillTreeView.NodeSize)
    private const float MinGap = 70f;       // px libres mínimos entre dos nodos
    private const float MaxLineUnits = 3.6f; // una conexión más larga que esto cruzaría media pantalla (el eje largo de Alucard mide 3,5)

    [TestCase("Assets/Data/Skills/Alucard_Tree.asset")]
    [TestCase("Assets/Data/Skills/Guts_Tree.asset")]
    public void Nodes_KeepAtLeastTheMinimumGapBetweenThem(string path)
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
        Assert.IsNotNull(tree, "Falta " + path);

        float minDistance = (NodeSize + MinGap) / UnitScale;
        for (int i = 0; i < tree.nodes.Length; i++)
        {
            for (int j = i + 1; j < tree.nodes.Length; j++)
            {
                float d = Vector2.Distance(tree.nodes[i].position, tree.nodes[j].position);
                Assert.GreaterOrEqual(d, minDistance - 0.01f,
                    tree.nodes[i].id + " y " + tree.nodes[j].id + " están a " + d.ToString("F2") + " unidades (mínimo " + minDistance.ToString("F2") + ")");
            }
        }
    }

    [TestCase("Assets/Data/Skills/Alucard_Tree.asset")]
    [TestCase("Assets/Data/Skills/Guts_Tree.asset")]
    public void Connections_AreNotAbsurdlyLong(string path)
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);

        foreach (SkillNode node in tree.nodes)
        {
            foreach (string other in node.connections)
            {
                SkillNode target = tree.Find(other);
                if (target == null) continue;

                float d = Vector2.Distance(node.position, target.position);
                Assert.LessOrEqual(d, MaxLineUnits, node.id + " - " + other + " es una línea de " + d.ToString("F2") + " unidades");
            }
        }
    }

    [TestCase("Assets/Data/Skills/Alucard_Tree.asset")]
    [TestCase("Assets/Data/Skills/Guts_Tree.asset")]
    public void Tree_HasAReadableShape_NotATinyBlob(string path)
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (SkillNode node in tree.nodes)
        {
            min = Vector2.Min(min, node.position);
            max = Vector2.Max(max, node.position);
        }

        // Con tantos nodos y este aire, el árbol ocupa bastante más que una pantalla: ancho y alto razonables.
        Assert.GreaterOrEqual(max.x - min.x, 12f, "el árbol es muy angosto");
        Assert.GreaterOrEqual(max.y - min.y, 5f, "el árbol es muy bajo");
    }
}
