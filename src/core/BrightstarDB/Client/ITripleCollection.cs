using System;
using System.Collections.Generic;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Defines a collection of <see cref="ITriple"/> instances with some high-level operations for matching/removing items.
    /// </summary>
    internal interface ITripleCollection
    {
        /// <summary>
        ///     Get an enumeration over the distinct subject URIs of the triples in the collection
        /// </summary>
        IEnumerable<string> Subjects { get; }

        /// <summary>
        ///     Get an enumeration over the <see cref="ITriple" /> instances in the collection
        /// </summary>
        IEnumerable<ITriple> Items { get; }

        /// <summary>
        ///     Adds a triple to the collection.
        /// </summary>
        /// <remarks>If an equal ITriple instance already exists in the collection the specified instance will not be added.</remarks>
        /// <param name="triple">The triple to be added</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="triple" /> is NULL</exception>
        void Add(ITriple triple);

        /// <summary>
        ///     Adds all of the members of an enumeration to the collection
        /// </summary>
        /// <remarks>Duplicate triples are not added to the collection</remarks>
        /// <param name="triples">The triples to be added to the collection</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="triples" /> is NULL</exception>
        void AddRange(IEnumerable<ITriple> triples);

        /// <summary>
        ///     Removes all triples in the collection that have a specific subject URI
        /// </summary>
        /// <param name="subject">The subject URI of the triples to be removed from the collection</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="subject" /> is NULL</exception>
        void RemoveBySubject(string subject);

        /// <summary>
        ///     Removes all triples in the collection that have a specific subject and predicate URI
        /// </summary>
        /// <param name="subject">The subject URI of the triples to be removed from the collection</param>
        /// <param name="predicate">The prediate URI of the triples to be removed from the collection</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="subject" /> or <paramref name="predicate" /> is NULL</exception>
        void RemoveBySubjectPredicate(string subject, string predicate);

        /// <summary>
        ///     Removes all triples in the collection that have a specific predicate and object URI
        /// </summary>
        /// <remarks>
        ///     This method only matches triples with resource values as the object. Triples with literal values
        ///     will not be removed by this method.
        /// </remarks>
        /// <param name="predicate">The predicate URI of the triples to be removed from the collection</param>
        /// <param name="obj">The object URI of the triples to be removed from the collection</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="obj" /> or <paramref name="predicate" /> is NULL</exception>
        void RemoveByPredicateObject(string predicate, string obj);

        /// <summary>
        ///     Removes all triples in the collection that have a specific object URI
        /// </summary>
        /// <remarks>
        ///     This method only matches triples with resource values as the object. Triples with literal values
        ///     will not be removed by this method.
        /// </remarks>
        /// <param name="obj">The object URI of the triples to be removed from the collection</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="obj" /> is NULL</exception>
        void RemoveByObject(string obj);

        /// <summary>
        ///     Removes all triples in the collection that have a specific subject, predicate and object URI
        /// </summary>
        /// <param name="subject">The subject URI of the triples to be removed</param>
        /// <param name="predicate">The predicate URI of the triples to be removed</param>
        /// <param name="obj">The object URI of the triples to be removed</param>
        /// <exception cref="ArgumentNullException">
        ///     Raised if <paramref name="subject" />, <paramref name="predicate" />, or
        ///     <paramref name="obj" /> is NULL
        /// </exception>
        void RemoveBySubjectPredicateObject(string subject, string predicate, string obj);

        /// <summary>
        ///     Removes all triples in the collection that have a specific subject, predicate and literal value
        /// </summary>
        /// <param name="subject">The subject URI of the triples to be removed</param>
        /// <param name="predicate">The predicate URI of the triples to be removed</param>
        /// <param name="literal">The literal value of the triples to be removed</param>
        /// <param name="dataType">The datatype URI of the triples to be removed</param>
        /// <param name="langCode">
        ///     The literal language code of the triples to be removed. A value of NULL will match triples with
        ///     any language code (or no language code)
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Raised if <paramref name="subject" />, <paramref name="predicate" />,
        ///     <paramref name="literal" />, or <paramref name="dataType" /> is NULL
        /// </exception>
        void RemoveBySubjectPredicateLiteral(string subject, string predicate, string literal, string dataType,
            string langCode);


        /// <summary>
        ///     Yields and enumeration over the triples in the collection that match the provided pattern.
        /// </summary>
        /// <param name="matchPattern">
        ///     The match pattern specified as a triple where a wildcard match for Subject, Predicate,
        ///     Object or Graph can be specified using NULL
        /// </param>
        /// <returns>An enumeration of <see cref="ITriple" /> instances that match the provided pattern</returns>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="matchPattern" /> is NULL</exception>
        IEnumerable<ITriple> GetMatches(ITriple matchPattern);

        /// <summary>
        ///     Yields an enumeration over all the triples in the collection with a specified subject URI
        /// </summary>
        /// <param name="subject">The subject of the triples to yield</param>
        /// <returns>An enumeration of <see cref="BrightstarDB.Model.ITriple" /> instances</returns>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="subject" /> is NULL</exception>
        IEnumerable<ITriple> GetMatches(string subject);

        /// <summary>
        ///     Yields an enumeration over all the triples in the collection with the specified
        ///     subject and predicate URIs.
        /// </summary>
        /// <param name="subject">The subject URI of the triples to match</param>
        /// <param name="predicate">The predicate URI of the triples to match</param>
        /// <returns>An enumeration of <see cref="ITriple" /> instances</returns>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="subject" /> or <paramref name="predicate" /> is NULL</exception>
        IEnumerable<ITriple> GetMatches(string subject, string predicate);

        /// <summary>
        ///     Yields an enumeration over all the triples in the collection with the specified
        ///     subject, predicate and object URIs.
        /// </summary>
        /// <remarks>
        ///     The enumeration yielded will not include triples with a literal value that matches <paramref name="obj" />
        /// </remarks>
        /// <param name="subject">The subject URI of the triples to match</param>
        /// <param name="predicate">The predicate URI of the triples to match</param>
        /// <param name="obj">The object URI of the triples to match</param>
        /// <returns>An enumeration of the matching <see cref="ITriple" /> instances</returns>
        /// <exception cref="ArgumentNullException">
        ///     Raised if <paramref name="subject" />, <paramref name="predicate" />, or
        ///     <paramref name="obj" /> is NULL
        /// </exception>
        IEnumerable<ITriple> GetMatches(string subject, string predicate, string obj);

        /// <summary>
        ///     Clear the contents of the collection
        /// </summary>
        void Clear();

        /// <summary>
        ///     Determine if the collection contains one or more triples with the specified subject URI
        /// </summary>
        /// <param name="subject">The subject URI to check for</param>
        /// <returns>True if the collection contains at least one triple with the specified subject URI, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="subject" /> is NULL</exception>
        bool ContainsSubject(string subject);

        /// <summary>
        ///     Get the total number of triples held in this collection
        /// </summary>
        /// <returns></returns>
        int Count();
    }
}