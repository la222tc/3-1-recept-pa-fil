using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }


        public void Load()
        {
            // Skapar en lista med referens till receptojekt
            List<IRecipe> listOfRecipies = new List<IRecipe>();
            // Instansiserer Enumtyperna
            RecipeReadStatus recipestatus = new RecipeReadStatus();
            Recipe recipe = null;

            try
            {
                // Öppnar en textfil för läsning
                using (StreamReader reader = new StreamReader(_path))
                {
                    string line;

                    // Läser filen rad för rad
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Om det är en avdelning för nytt recept
                        if (line == SectionRecipe)
                        {
                            recipestatus = RecipeReadStatus.New;         // sätt status till att nästa rad som läses in kommer att vara receptets namn
                        }

                        // eller om det är avdelningen för ingredienser
                        else if (line == SectionIngredients)
                        {
                            recipestatus = RecipeReadStatus.Ingredient;  // sätt status till att kommande rader som läses in kommer att vara receptets ingredienser
                        }

                        // eller om det är avdelningen för instruktioner
                        else if (line == SectionInstructions)
                        {
                            recipestatus = RecipeReadStatus.Instruction; // sätt status till att kommande rader som läses in kommer att vara receptets instruktioner
                        }

                        // annars är det ett namn, en ingrediens eller en instruktion
                        else
                        {

                            // Om status är satt att raden ska tolkas som ett recepts namn
                            if (recipestatus == RecipeReadStatus.New)
                            {
                                recipe = new Recipe(line);               // Skapa nytt receptobjekt med receptets namn
                                listOfRecipies.Add(recipe);
                            }

                            // eller om status är satt att raden ska tolkas som en ingrediens
                            else if (recipestatus == RecipeReadStatus.Ingredient)
                            {
                                string[] values = line.Split(';');      // Delar upp raden i delar


                                // Om antalet delar inte är tre
                                if (values.Length != 3)
                                {
                                    throw new FileFormatException();    // Kasta ett undantag
                                }

                                // Skapar ett ingrediensobjekt och initiera det med de tre delarna för mängd, mått och namn
                                Ingredient ingredients = new Ingredient();
                                ingredients.Amount = values[0];
                                ingredients.Measure = values[1];
                                ingredients.Name = values[2];
                                recipe.Add(ingredients);            // Lägg till ingrediensen till receptets lista med ingredienser
                            }

                            // eller om status är satt att raden ska tolkas som en instruktion
                            else if (recipestatus == RecipeReadStatus.Instruction)
                            {
                                recipe.Add(line);       // Lägger till raden till receptets lista med instruktioner
                            }

                            else 
                            {
                                throw new FileFormatException();
                            }
                        }
                    }
                }

                // Sorterar Listan av recept och tilldelar avsett fält i klassen, _recipes, en referens till listan
                _recipes = listOfRecipies.OrderBy(r => recipe.Name).ToList();
                IsModified = false;                         // Tilldelar IsModified värdet false som indikerar att listan med recept är oförändrad
                OnRecipesChanged(EventArgs.Empty);          // Utlös händelse om att recept har lästs in genom att anropa metoden OnRecipesChanged och skicka med parametern EventArgs.Empty

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Save()
        {
            try
            {
                using (StreamWriter write = new StreamWriter(_path))
                {
                    foreach (Recipe recipe in _recipes)
                    {
                        // Skriver ut konstanten SectionRecipe och recept namnet
                        write.WriteLine(SectionRecipe);
                        write.WriteLine(recipe.Name);


                        // Skriver ut konstanten SectionIngredients och ingredienserna med ; i mellan Amount, Measure och Name
                        write.WriteLine(SectionIngredients);
                        foreach (Ingredient ingredients in recipe.Ingredients)
                        {
                            write.WriteLine("{0};{1};{2}", ingredients.Amount, ingredients.Measure, ingredients.Name);
                        }

                        // Skriver ut konstanten SectionInstructions och instruktionerna
                        write.WriteLine(SectionInstructions);
                        foreach (string instructions in recipe.Instructions)
                        {
                            write.WriteLine(instructions);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
