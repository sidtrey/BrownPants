# NHibernate Candidate Signal Contract

The `inspect` report's architecture pattern section adds bounded `Candidate` signals for explicit NHibernate project/package metadata, mapping/configuration XML, and source-visible C#/VB API patterns.

Candidates include relative path, line where available, language or metadata kind, category, and a short evidence identifier. Existing signal limits and Markdown/JSON output stability apply.

Analysis reads files only. Project files are parsed as XML; imported MSBuild files and conditions are not evaluated. It never restores packages, loads assemblies, accesses a database, executes source, accesses the network, or writes to the inspected repository. Candidate evidence does not prove transitive dependency, runtime configuration, mapping activation, session scope, or database effects.
