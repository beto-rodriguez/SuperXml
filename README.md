# Templator

Templator is just a light weight and easy to use template engine library, useful to create string, Xml and Html Templates(working on it).

Why another template engine?
  * Multi-type support.
  * Math expressions.
  * **AngularJs (1.*)**-like markup, if you are familiar with it you’re are familiar with this library.
  * Support for nested elements.
  * Expression filters, for example make an integer `24` compile like `$24.00`.
  * Open source, do whatever you need with this code, improve it (please), remove features, for commercial and no commercial purposes. [License here](https://github.com/beto-rodriguez/Templator/blob/master/LICENSE.txt).

#Install
From visual studio go to `Tools` -> `Nuget Package Manager` -> `Package Manager Console` then in the `Package Manager console` write the next command.
```
Install-Package Tor
```
Once it is installed you can use the `Compiler` class, you can find it at namespace `Templator`.
#How to use it?
 1. Create a `Compiler` class
 2. Add as many elements to the `Scope` as you need
 3. Feed your `Template` and get the result

# Example 1, Hello World
```c#
// 1. Create a compiler class
Compiler compiler = new Compiler(); 

// 2. Add Elements to your Scope, the first parameter is key, second is value
//      key:    the 'variable name' for the compiler
//      value:  the value of the variable in this case the string "world"
compiler.AddElementToScope("name", "world")

//3. Call the compile Method and feed the template t get the result
string result = compiler.CompileString("Hello {{name}}!");
//now results contains a "Hello wolrd!"
```
#Example 2, multiple scope elements
For example **2.a** and **2.b** we are going to use the scope defined below
```c#
var compiler = new Compiler()
                .AddElementToScope("name", "Excel")
                .AddElementToScope("width", 100)
                .AddElementToScope("height", 500)
                .AddElementToScope("bounds", new[] {10, 0, 10, 0})
                .AddElementToScope("elements", new []
                {
                    new { name = "John", age= 10 },
                    new { name = "Maria", age= 57 },
                    new { name = "Mark", age= 23 },
                    new { name = "Edit", age= 82 },
                    new { name = "Susan", age= 37 }
                });
```
After `Scope` is ready all you need to do is call the `Compile` method according to your needs
```c#
compiler.Compile("Hello {{name}}") // a string
compiler.CompileXml(@"c:/.../file.xml"); //a xml file
compiler.CompileXml(new StringReader("<doc><.../></doc>"));//a xml string
```
#2.a Compile it from a string template 
**Template**
```xml
Hello {{name}}, you are a document with a size of {{width}}x{{height}} and an 
area of {{width*height}}

now here is a list with your bounds:
  <trRun TrRepeat="b in bounds">-value {{$index}}: {{b}}
  </trRun>

now here you can see a filtered list of classes
  <trRun trRepeat="e in elements" trIf="e.age > 25">-{{e.name}}, age {{e.age}}
  </trRun>
```
**Result:**
```
Hello Excel, you are a document with a size of 100x500 and an area of 50000

now here is a list with your bounds:
  -value 0: 10
  -value 1: 0
  -value 2: 10
  -value 3: 0
  

now here you can see a filtered list of classes
  -Maria, age 57
  -Edit, age 82
  -Susan, age 37
```
this is how the code should look
```c#
var template = "...a string containing the template of above..."
var compiled = new Compiler()
                .AddElementToScope("name", "Excel")
                .AddElementToScope("width", 100)
                .AddElementToScope("height", 500)
                .AddElementToScope("bounds", new[] {10, 0, 10, 0})
                .AddElementToScope("elements", new []
                {
                    new { name = "John", age= 10 },
                    new { name = "Maria", age= 57 },
                    new { name = "Mark", age= 23 },
                    new { name = "Edit", age= 82 },
                    new { name = "Susan", age= 37 }
                })
                .CompileString(template);
  //compiled now contains the string of the result above
```
#2.b from a Xml File
**Template**
```xml
<document>
  <name>my name is {{name}}</name>
  <width>{{width}}</width>
  <height>{{height}}</height>
  <area>{{width*height}}</area>
  <padding>
    <bound trRepeat="bound in bounds">{{bound}}</bound>
  </padding>
  <content>
    <element ForEach="element in elements" If="element.age > 25">
      <name>{{element.name}}</name>
      <age>{{element.age}}</age>
    </element>
  </content> 
</document>
```
**Result**
```xml
<document>
  <name>my name is Excel</name>
  <width>100</width>
  <height>500</height>
  <area>50000</area>
  <padding>
    <bound>10</bound>
    <bound>0</bound>
    <bound>10</bound>
    <bound>0</bound>
  </padding>
  <content>
    <element>
      <name>Maria</name>
      <age>57</age>
    </element>
    <element>
      <name>Edit</name>
      <age>82</age>
    </element>
    <element>
      <name>Susan</name>
      <age>37</age>
    </element>
  </content>
</document>
```
The only difference from **2.a** is that now you need to call `CompileXml()` method, because source is now Xml,
`CompileXml()` can be called with the next parameters:
  1. `compiler.CompileXml(@"C:\...\myXml.xml")` a string, indicating the path of the XmlFile
  2. `compiler.CompileXml(new StringReader("<doc><.../></doc>"));` a StringReader initialized with the template
  3. from a stream
  4. from a Custom XmlReader Class

# Example 3, multiple features
Consider next Xml as template, and `numbers` is an array of integers containing only 2 elements (0, 1)
```xml
<doc>
<trRun trRepeat="a in numbers">
    <trRun trRepeat="b in numbers" >
		    <element row="{{$parent.$index+1}}" column="{{$index+1}}">
			     a) from a local scope variable {{a | currency}}, {{b}}
			     b) from an array: {{numbers[0]}}
			     c) from parent scope: {{$parent.$index}}
			     d) is it even? {{if($even, 'yes', 'nope')}}
		   </element>      
    </trRun>
  </trRun>
</doc>
```
will compile as
```xml
<doc>
<element row="1" column="1">
          a) from a local scope variable $0.00, 0
          b) from an array: 0
          c) from parent scope: 0
          d) is it even? yes
  </element>
  <element row="1" column="2">
          a) from a local scope variable $0.00, 1
          b) from an array: 0
          c) from parent scope: 0
          d) is it even? nope
  </element>
  <element row="2" column="1">
          a) from a local scope variable $1.00, 0
          b) from an array: 0
          c) from parent scope: 1
          d) is it even? yes
  </element>
  <element row="2" column="2">
          a) from a local scope variable $1.00, 1
          b) from an array: 0
          c) from parent scope: 1
          d) is it even? nope
  </element>
</doc>
 ```
#HTML
Coming Soon...
#trIf Command
Evaluates if the element should be included according to a condition. A condition can include everything supported by ncalc (most of common things). **Examples**:
* `<MyElement trIf="10 > 6"/>` numeric.
* `<MyElement trIf="aValueFromScope == 'visible'"/>` string and from scope
* `<MyElement trIf="10 > h && aValueFromScope == 'visible'"/>` another example

`trIf ` is useful when you need to include or ignore a specific element but what happens if you need for example to decide an Xml attribute according to a condition?
 
In that case you should use NCalc `if` function example: 

`<Element type="{{if(10 == 5, '10 is equals to 5', '10 is diferent to 5')}}"></Element>`

#trRepeat Command
Repeats the element the same number of times as items in the array.

**Example**

Consider `numbers` an array of integers in the Scope

`<MyElement trRepeat="number in numbers" myAttribute="{{number}}" />`

**Result**
```xml
<MyElement myAttribute="1" />
<MyElement myAttribute="2" />
<MyElement myAttribute="3" />
... 
```
Each repeated element has some extra Scope items:
 * `$index` 	a cero based integer that indicates its position on repeater.
 * `$even` 	a boolean value indicating if the position on the repeater even.
 * `$odd` 	a boolean value indicating if the position on the repeater odd.
 * `$parent` 	parent scope. 

**Input**
```xml
<trRun trRepeat="a in numbers">
    <trRun trRepeat="b in numbers" >
		    <element row="{{$parent.$index+1}}" column="{{$index+1}}">
			     a) from a local scope variable {{a | currency}}, {{b}}
			     b) from an array: {{numbers[0]}}
			     c) from parent scope: {{$parent.$index}}
			     d) is it even? {{if($even, 'yes', 'nope')}}
		   </element>      
    </trRun>
  </trRun>
```
**Result**
```xml
<element row="1" column="1">
          a) from a local scope variable $0.00, 0
          b) from an array: 0
          c) from parent scope: 0
          d) is it even? yes
  </element>
  <element row="1" column="2">
          a) from a local scope variable $0.00, 1
          b) from an array: 0
          c) from parent scope: 0
          d) is it even? nope
  </element>
  <element row="2" column="1">
          a) from a local scope variable $1.00, 0
          b) from an array: 0
          c) from parent scope: 1
          d) is it even? yes
  </element>
  <element row="2" column="2">
          a) from a local scope variable $1.00, 1
          b) from an array: 0
          c) from parent scope: 1
          d) is it even? nope
  </element>
 ```
#trRun Command
`trRun` is useful when you need to run a command on a set of Xml elements or just when you need for example to write a string according to a condition. `trRun` is ignored when compiled.

**Example 1** use it to run `trRepeater` on a group of elements
```xml
<Document>
  <trRun trRepeat="number in numbers">
    <text1></text1>
    <text2></text2>
    <text3></text3>
  </trRun>
  <trRun trIf="8 > 7">
    <text1></text1>
    <text2></text2>
    <text3></text3>
  </trRun>
</Document>
```
**Example2** Writing a string according to condition
```xml
Hello I need a:
<trRun trIf="user.age >= 18">
  beer
</trRun>
<trRun trIf="user.age < 18">
  juice
</trRun>
```
#Filters
Filters is an easy way to display an expression in a custom format. for example when you have a decimal value `102.312` and you need it to display it as currency, all you need to do is use an expression as 

`{{102.312 | currency}}`

And you will get `$102.31`. **Tor** includes already the next filters:
  * `currency`: it takes a numeric value and returns `input.ToString("C")`.

You can add as many filters as you need adding elements to `Filters` dictionary of the static `Compiler` class.
**Example:**
```c#
//consider that you can’t add a repeated element to a dictionary
//so when you add a filter be sure that this code is only hit once
Compiler.Filters.Add("helloFilter", input =>
            {
                return "Hello " + input;
            });
```
After you added your filter you can use it in your markup.
```c#
var compiled = new Compiler().AddElementToScope("elements", new []
                {
                    new User {Name = "John", Age=13},
                    new User {Name = "Maria", Age=57},
                    new User {Name = "Mark", Age=23},
                    new User {Name = "Edit", Age=82},
                    new User {Name = "Susan", Age=37}
                }).CompileString();
```
**Input**
```xml
<trRun trRepeat="e in elements">
  {{e.Name | helloFilter}}
 </trRun>
```
**Output**
```

  Hello John
 
  Hello Maria
 
  Hello Mark
 
  Hello Edit
 
  Hello Susan
 
```
Use filters whenever you need to change the output of a expression. another application could be to return for example input times 2.

#Math and Logical Operators
math operations are evaluated by Ncalc, basically it works with the same syntax used in C#. For more info go to https://ncalc.codeplex.com/
```xml
<Document>
  <Math>
    2 + 2 = {{2+2}}, 2 x 2 = {{2*2}}, 2 / 2 = {{2/2}},
    (2+2)/(2+2/2)x2 = {{(2+2)/(2+2/2)*2}}
  </Math>
  <Logical>
    2 > 5 {{2>5}}, 4 = 9 {{ 4 == 9 }},
    for strings use '', for example (hola = hello {{'hello' == 'hello'}})
  </Logical>
  <condition>{{ if(1 == 0, 'yes it is!', 'nope') }}</condition>
</Document>
```
Compiled
```xml
<Document>
  <Document>
  <Math>
    2 + 2 = 4, 2 x 2 = 4, 2 / 2 = 1,
    (2+2)/(2+2/2)x2 = .66
  </Math>
  <Logical>
    2 > 5 False, 4 = 9 False,
    for strings use '', for example (hello = hello True)
  </Logical>
  <condition>nope</condition>
</Document>
</Document>
```
#Dot Notation
Dot notation is useful when you add classes to compiler Scope, in the next example we added an User class with a string property `name`, a string property `lastName` and a integer property `age`, you can add any type and nest as many classes as necessary.
Input XML
```xml
<Document>
  <Text>
    {{user.name}} {{user.lastName}}, age: {{user.age}}
  </Text>
</Document>
```
Compiled
```xml
<Document>
  <Text>
    Roger Martinez, age: 20
  </Text>
</Document>
```
#Supported Types:
When you use `.AddElementToScope(Key, Value)`, Value is dynamic, that means that it will be evaluated at runtime, so 
It should support all kind of types, enums, classes, all elements and commands could be nested with no problem.
#Performance
from `<element ForEach="element in elements">{{element}}</element>` and elements equals to an array of 10,000 integers Core i5 @ 2.3 GHz took an average of 300 ms to compile in release.

Sometimes Xml files contains elements that you don’t need to compile. to improve performance compile only what you need.
```c#
var onlyContet = compiler.CompileXml(new StringReader(SourceBox.Text), 
                 x => x.Children.First(y => y.Name == "content")); 
```

#Debug
When a property is not found in the Compiler Scope, Compiler will let you know which name could not be found. It uses `Trace.WriteLine()`, so in visual studio you will find it in the output window.

**Warning** 
```
When a property is not found the impact in performance is huge!
```
